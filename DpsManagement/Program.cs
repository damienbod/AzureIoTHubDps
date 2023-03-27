using CertificateManager;
using CertificateManager.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace DpsManagement;

class Program
{
    static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
    static readonly string _pathToCerts = $"{_directory}/../../../../Certs/";
    static ServiceProvider? _sp;

    static async Task Main(string[] args)
    {
        InitServices();

        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        #region Individual Enrollment

        /// -- DPS Create individual enrollment
        var deviceId = "testdevice01";
        var dpsIndividualEnrollmentService = _sp.GetService<DpsIndividualEnrollment>();
        //if (dpsIndividualEnrollmentService == null) throw new ArgumentNullException(nameof(dpsIndividualEnrollmentService));
        
        var dpsEnrollmentPem = new X509Certificate2($"{_pathToCerts}{deviceId}.pem");
        //await dpsIndividualEnrollmentService.CreateIndividualEnrollment(deviceId, dpsEnrollmentPem);

        var certificateTestdevice01 = new X509Certificate2($"{_pathToCerts}{deviceId}.pfx", "1234");
        //await CreateIndividualEnrollmentDeviceAsync(deviceId, certificateTestdevice01);

        #endregion

        #region Group Enrollment

        /// -- DPS Create Enrollment Group
        var dpsEnrollmentGroupService = _sp.GetService<DpsEnrollmentGroup>();
        if (dpsEnrollmentGroupService == null) throw new ArgumentNullException(nameof(dpsEnrollmentGroupService));

        //var dpsGroupEnrollmentPem = new X509Certificate2($"{_pathToCerts}dpsIntermediate1.pem");
        //await dpsEnrollmentGroupService.CreateDpsEnrollmentGroupAsync("dpsIntermediate1", dpsGroupEnrollmentPem);

        /// --DPS create certificate, then enrollment group
        var dpsCaCertificate = new X509Certificate2($"{_pathToCerts}dpsCa.pfx", "1234");

        var commonNameAndGroupEnrollmentName = "engroup4";
        var grouprEnrollmentCertificate = await CreateEnrollmentGroupAndNewDeviceCert(commonNameAndGroupEnrollmentName, dpsCaCertificate);

        //  Common Name "CN=" value within the device x.509 certificate MUST match the Group Enrollment name within DPS.
        await CreateGroupEnrollmentDeviceAsync(commonNameAndGroupEnrollmentName, grouprEnrollmentCertificate, "1234");

        /// --Create certificate, register device to dps and create in iot hub
        //var dpsIntermediate1Certificate = new X509Certificate2($"{_pathToCerts}dpsIntermediate1.pfx", "1234");
        //await CreateGroupEnrollmentDeviceAsync("groupddevice01-intermediate1", dpsIntermediate1Certificate, "1234");

        await dpsEnrollmentGroupService.QueryEnrollmentGroupAsync();

        #endregion

        /// -- DISABLE / ENABLE IoT Hub Device
        //var ioTHubUpdateDevice = _sp.GetService<IoTHubUpdateDevice>();
        //var thumbprint = "861FA67FB61BAEC0B950B69FF1D643D80554D3E8";
        //await ioTHubUpdateDevice.UpdateAuthDeviceCertificateAuthorityAsync("testdevice01", thumbprint);
        //await ioTHubUpdateDevice.DisableDeviceAsync("testdevice01");
        //await ioTHubUpdateDevice.EnableDeviceAsync("testdevice01");

        /// -- DISABLE / ENABLE DPS EnrollmentGroup
        //var dpsUpdateDevice = _sp.GetService<DpsUpdateDevice>();
        //await dpsUpdateDevice.DisableEnrollmentGroupAsync("dpsIntermediate1");
        //await dpsUpdateDevice.EnableEnrollmentGroupAsync("dpsIntermediate1");
    }

    private static async Task<X509Certificate2> CreateEnrollmentGroupAndNewDeviceCert(
        string enrollmentGroup, X509Certificate2 parentCert)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsEnrollmentGroup = _sp.GetService<DpsEnrollmentGroup>();
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsEnrollmentGroup == null) throw new ArgumentNullException(nameof(dpsEnrollmentGroup));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        var enrollmentGroupCert = cc.NewIntermediateChainedCertificate(
           new DistinguishedName { CommonName = enrollmentGroup },
           new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
           2, enrollmentGroup, parentCert);
        enrollmentGroupCert.FriendlyName = $"{enrollmentGroup} certificate";

        var enrollmentGroupPEM = iec.PemExportPublicKeyCertificate(enrollmentGroupCert);
        var cert = iec.PemImportCertificate(enrollmentGroupPEM);

        await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync(
            enrollmentGroup, new X509Certificate2(cert));

        var deviceInPfxBytes = iec.ExportChainedCertificatePfx("1234", enrollmentGroupCert, parentCert);
        File.WriteAllBytes($"{enrollmentGroup}.pfx", deviceInPfxBytes);

        var devicePEM = iec.PemExportPublicKeyCertificate(enrollmentGroupCert);
        File.WriteAllText($"{enrollmentGroup}.pem", devicePEM);

        return enrollmentGroupCert;
    }

    private static async Task<DeviceRegistrationResult?> CreateIndividualEnrollmentDeviceAsync(
     string deviceId, X509Certificate2 certificate)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsRegisterDevice = _sp.GetService<DpsRegisterDevice>();
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsRegisterDevice == null) throw new ArgumentNullException(nameof(dpsRegisterDevice));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        deviceId = deviceId.ToLower();

        var result = await dpsRegisterDevice.RegisterDeviceAsync(certificate, certificate);

        return result;
    }

    private static async Task<X509Certificate2> CreateGroupEnrollmentDeviceAsync(
        string commonNameDeviceId, X509Certificate2 dpsGroupCertificate, string password)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsRegisterDevice = _sp.GetService<DpsRegisterDevice>();
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsRegisterDevice == null) throw new ArgumentNullException(nameof(dpsRegisterDevice));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        commonNameDeviceId = commonNameDeviceId.ToLower();

        var device = cc.NewDeviceChainedCertificate(
          new DistinguishedName { CommonName = commonNameDeviceId },
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
          commonNameDeviceId, dpsGroupCertificate);
        device.FriendlyName = $"IoT device {commonNameDeviceId}";
        
        var deviceInPfxBytes = iec.ExportChainedCertificatePfx(password, device, dpsGroupCertificate);
        var deviceCert = new X509Certificate2(deviceInPfxBytes, password);

        //var deviceExported = new X509Certificate2(device.Export(X509ContentType.Pfx));
        
        await dpsRegisterDevice.RegisterDeviceAsync(deviceCert, deviceCert);

        return device;
    }

    private static void InitServices()
    {
        ILoggerFactory loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
              $@"../../../dps.logs",
              fileSizeLimitBytes: 1_000_000,
              rollOnFileSizeLimit: true,
              shared: true)
            .CreateLogger();

        _sp = new ServiceCollection()
            .AddCertificateManager()
            .AddSingleton<IConfiguration>(GetConfig())
            .AddTransient<DpsRegisterDevice>()
            .AddTransient<DpsEnrollmentGroup>()
            .AddTransient<DpsIndividualEnrollment>()
            .AddTransient<IoTHubUpdateDevice>()
            .AddTransient<DpsUpdateDevice>()
            .AddSingleton(loggerFactory)
            .BuildServiceProvider();
    }

    private static IConfigurationRoot GetConfig()
    {
        var location = Assembly.GetEntryAssembly()!.Location;
        var directory = Path.GetDirectoryName(location);
        var config = new ConfigurationBuilder();

        config.AddJsonFile($"{directory}{Path.DirectorySeparatorChar}appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets("6556e69f-ba37-48cb-aad5-643d5620c84b");

        var builder = config.Build();
        return builder;
    }
}
