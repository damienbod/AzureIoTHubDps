using CertificateManager;
using CertificateManager.Models;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace DpsManagement
{
    class Program
    {
        static string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static string pathToCerts = $"{directory}/../../../../Certs/";
        static async Task Main(string[] args)
        {
            var sp = GetServices();

            /// DPS Create Enrollment Group
            //var dpsEnrollmentGroup = sp.GetService<DpsEnrollmentGroup>();
            //var dpsEnrollmentCertificate = new X509Certificate2($"{pathToCerts}dpsIntermediate1.pem");
            //await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync("dpsIntermediate1", dpsEnrollmentCertificate);
            
            /// DPS create certificte, then enrollment group
            //var dpsCaCertificate = new X509Certificate2($"{pathToCerts}dpsCa.pfx", "1234");
            //var cert = await CreateEnrollmentGroup("engroup", dpsCaCertificate, "1234");

            /// DPS Create individual enrollment
            //var dpsIndividualEnrollment = sp.GetService<DpsIndividualEnrollment>();
            //var dpsEnrollmentCertificate = new X509Certificate2($"{pathToCerts}testdevice01.pem");
            //await dpsIndividualEnrollment.CreateIndividualEnrollment("testdevice01", dpsEnrollmentCertificate);

            /// Create certificate, register device to dps and create in iot hub
            var dpsIntermediate1 = new X509Certificate2($"{pathToCerts}dpsIntermediate1.pfx", "1234");
            await CreateDeviceAsync("will", dpsIntermediate1, "1234");
            //await CreateDeviceAsync("yes", dpsIntermediate1, "1234");

            //await dpsEnrollmentGroup.QueryEnrollmentGroupAsync().ConfigureAwait(false);

            /// DISABLE / ENABLE IoT Hub Device
            //var ioTHubUpdateDevice = sp.GetService<IoTHubUpdateDevice>();
            //await ioTHubUpdateDevice.UpdateAuthDeviceCertificateAuthorityAsync("testdevice01", null);
            //await ioTHubUpdateDevice.DisableDeviceAsync("testdevice01");
            //await ioTHubUpdateDevice.EnableDeviceAsync("testdevice01");

            /// DISABLE / ENABLE DPS EnrollmentGroup
            //var dpsUpdateDevice = sp.GetService<DpsUpdateDevice>();
            //await dpsUpdateDevice.DisableEnrollmentGroupAsync("dpsIntermediate1");
            //await dpsUpdateDevice.EnableEnrollmentGroupAsync("dpsIntermediate1");
        }

        private static async Task<X509Certificate2> CreateEnrollmentGroup(
            string enrollmentGroup, 
            X509Certificate2 parentCert, 
            string password)
        {
            var sp = GetServices();
            var cc = sp.GetService<CreateCertificatesClientServerAuth>();
            var dpsEnrollmentGroup = sp.GetService<DpsEnrollmentGroup>();
            var iec = sp.GetService<ImportExportCertificate>();

            var enrollmentGroupCert = cc.NewIntermediateChainedCertificate(
               new DistinguishedName { CommonName = enrollmentGroup },
               new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
               2, enrollmentGroup, parentCert);
            enrollmentGroupCert.FriendlyName = $"{enrollmentGroup} certificate";

            var enrollmentGroupInPfxBytes = iec.ExportChainedCertificatePfx(password, enrollmentGroupCert, parentCert);
            var enrollmentGroupCertPfx = new X509Certificate2(enrollmentGroupInPfxBytes, password);
            var enrollmentGroupPEM = iec.PemExportPublicKeyCertificate(enrollmentGroupCertPfx);
            var cert = iec.PemImportCertificate(enrollmentGroupPEM);
            var dpsEnrollment = new X509Certificate2(cert);

            await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync(enrollmentGroup, dpsEnrollment);
            return enrollmentGroupCertPfx;
        }

        private static async Task<X509Certificate2> CreateDeviceAsync(
            string deviceId, 
            X509Certificate2 parentCertificate,
            string password)
        {
            deviceId = deviceId.ToLower();
            var sp = GetServices();
            var cc = sp.GetService<CreateCertificatesClientServerAuth>();
            var dpsRegisterDevice = sp.GetService<DpsRegisterDevice>();
            var iec = sp.GetService<ImportExportCertificate>();

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


        private static ServiceProvider GetServices()
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

            var serviceProvider = new ServiceCollection()
                .AddCertificateManager()
                .AddSingleton<IConfiguration>(GetConfig())
                .AddTransient<DpsRegisterDevice>()
                .AddTransient<DpsEnrollmentGroup>()
                .AddTransient<DpsIndividualEnrollment>()
                .AddTransient<IoTHubUpdateDevice>()
                .AddTransient<DpsUpdateDevice>()
                .AddSingleton(loggerFactory)
                .BuildServiceProvider();

            return serviceProvider;
        }

        private static IConfigurationRoot GetConfig()
        {
            var location = Assembly.GetEntryAssembly().Location;
            var directory = Path.GetDirectoryName(location);
            var config = new ConfigurationBuilder();

            config.AddJsonFile($"{directory}{Path.DirectorySeparatorChar}appsettings.json", optional: false, reloadOnChange: true)
                    .AddUserSecrets("6556e69f-ba37-48cb-aad5-643d5620c84b");

            var builder = config.Build();
            return builder;
        }
    }
}
