using CertificateManager;
using CertificateManager.Models;
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
        //var deviceId = "testdevice01";
        //var dpsIndividualEnrollmentService = _sp.GetService<DpsIndividualEnrollment>();
        //if (dpsIndividualEnrollmentService == null) throw new ArgumentNullException(nameof(dpsIndividualEnrollmentService));
        
        //var dpsEnrollmentPem = new X509Certificate2($"{_pathToCerts}{deviceId}.pem");
        //await dpsIndividualEnrollmentService.CreateIndividualEnrollment(deviceId, dpsEnrollmentPem);

        //var certificateTestdevice01 = new X509Certificate2($"{_pathToCerts}{deviceId}.pfx", "1234");
        //await CreateIndividualEnrollmentDeviceAsync(certificateTestdevice01);

        #endregion

        #region Group Enrollment

        /// -- DPS Create Enrollment Group
        var dpsEnrollmentGroupService = _sp.GetService<DpsEnrollmentGroup>();
        if (dpsEnrollmentGroupService == null) throw new ArgumentNullException(nameof(dpsEnrollmentGroupService));

        /// -- Use DPS create certificate to create enrollment group
        /// -- This cert must be registered with the DPS in the certificates blade
        var dpsEnrollmentPem = new X509Certificate2($"{_pathToCerts}dpsCa.pem");
        var dpsCaCertificate = new X509Certificate2($"{_pathToCerts}dpsCa.pfx", "1234");

        var commonNameAndGroupEnrollmentName = "enrollment-group";
        await CreateEnrollmentGroup(commonNameAndGroupEnrollmentName, dpsEnrollmentPem);

        await CreateGroupEnrollmentDeviceAsync("enrollment-group-device-01", dpsCaCertificate, "1234");
        await CreateGroupEnrollmentDeviceAsync("enrollment-group-device-02", dpsCaCertificate, "1234");

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
        //await dpsUpdateDevice.DisableEnrollmentGroupAsync(commonNameAndGroupEnrollmentName);
        //await dpsUpdateDevice.EnableEnrollmentGroupAsync(commonNameAndGroupEnrollmentName);
    }

    private static async Task<DeviceRegistrationResult?> CreateIndividualEnrollmentDeviceAsync(
        X509Certificate2 certificate)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsRegisterDevice = _sp.GetService<DpsRegisterDevice>();
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsRegisterDevice == null) throw new ArgumentNullException(nameof(dpsRegisterDevice));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        var result = await dpsRegisterDevice.RegisterDeviceAsync(certificate, certificate);

        return result;
    }

    private static async Task CreateEnrollmentGroup(string enrollmentGroup, X509Certificate2 groupCertificate)
    {
        if (_sp == null) throw new ArgumentNullException(nameof(_sp));

        var cc = _sp.GetService<CreateCertificatesClientServerAuth>();
        var dpsEnrollmentGroup = _sp.GetService<DpsEnrollmentGroup>();
        var iec = _sp.GetService<ImportExportCertificate>();

        if (cc == null) throw new ArgumentNullException(nameof(cc));
        if (dpsEnrollmentGroup == null) throw new ArgumentNullException(nameof(dpsEnrollmentGroup));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync(
            enrollmentGroup, new X509Certificate2(groupCertificate));
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
          new DistinguishedName { CommonName = $"{commonNameDeviceId}" },
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
          $"{commonNameDeviceId}", dpsGroupCertificate);
        device.FriendlyName = $"IoT device {commonNameDeviceId}";
        
        var deviceInPfxBytes = iec.ExportChainedCertificatePfx(password, device, dpsGroupCertificate);
        var deviceCert = new X509Certificate2(deviceInPfxBytes, password);

        await dpsRegisterDevice.RegisterDeviceAsync(deviceCert, dpsGroupCertificate);

        // Save File to use in IoC device
        File.WriteAllBytes($"{commonNameDeviceId}.pfx", deviceInPfxBytes);
        var devicePEM = iec.PemExportPublicKeyCertificate(device);
        File.WriteAllText($"{commonNameDeviceId}.pem", devicePEM);

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
