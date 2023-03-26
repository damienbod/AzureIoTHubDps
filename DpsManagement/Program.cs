using CertificateManager;
using CertificateManager.Models;
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

        /// -- DPS Create Enrollment Group
        //var dpsEnrollmentGroup = _sp.GetService<DpsEnrollmentGroup>();
        //var dpsEnrollmentCertificate = new X509Certificate2($"{_pathToCerts}dpsIntermediate1.pem");
        //await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync("dpsIntermediate1", dpsEnrollmentCertificate);

        /// -- DPS create certificte, then enrollment group
        //var dpsCaCertificate = new X509Certificate2($"{_pathToCerts}dpsCa.pfx", "1234");
        //var cert = await CreateEnrollmentGroup("engroup2", dpsCaCertificate);

        /// -- DPS Create individual enrollment
        var dpsIndividualEnrollment = _sp.GetService<DpsIndividualEnrollment>();
        var dpsEnrollmentCertificate = new X509Certificate2($"{_pathToCerts}testdevice01.pem");
        await dpsIndividualEnrollment.CreateIndividualEnrollment("testdevice01", dpsEnrollmentCertificate);

        /// -- Create certificate, register device to dps and create in iot hub
        //var dpsIntermediate1 = new X509Certificate2($"{_pathToCerts}dpsIntermediate1.pfx", "1234");
        //await CreateDeviceAsync("will4", dpsIntermediate1, "1234");
        //await CreateDeviceAsync("yes", dpsIntermediate1, "1234");

        //await dpsEnrollmentGroup.QueryEnrollmentGroupAsync();

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

    private static async Task<X509Certificate2> CreateEnrollmentGroup(
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
        return enrollmentGroupCert;
    }

    private static async Task<X509Certificate2> CreateDeviceAsync(
        string deviceId, X509Certificate2 parentCertificate, string password)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsRegisterDevice = _sp.GetService<DpsRegisterDevice>(); ;
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsRegisterDevice == null) throw new ArgumentNullException(nameof(dpsRegisterDevice));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        deviceId = deviceId.ToLower();

        var device = cc.NewDeviceChainedCertificate(
            new DistinguishedName { CommonName = deviceId },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
            deviceId, parentCertificate);
        device.FriendlyName = $"IoT device {deviceId}";
        
        var deviceInPfxBytes = iec.ExportChainedCertificatePfx(password, device, parentCertificate);
        var deviceCert = new X509Certificate2(deviceInPfxBytes, password);

        await dpsRegisterDevice.RegisterDeviceAsync(deviceCert, parentCertificate);

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
