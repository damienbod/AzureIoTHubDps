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
        static async Task Main(string[] args)
        {
            var location = Assembly.GetEntryAssembly().Location;
            var directory = Path.GetDirectoryName(location);
            var sp = GetServices();
            var pathToCerts = $"{directory}/../../../../Certs/";

            var dpsEnrollmentGroup = sp.GetService<DpsEnrollmentGroup>();
            var dpsEnrollmentCertificate = new X509Certificate2($"{pathToCerts}dpsIntermediate2.pem");
            await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync("dpsIntermediate2", dpsEnrollmentCertificate);

            var dpsRegisterDevice = sp.GetService<DpsRegisterDevice>();
            X509Certificate2 deviceCertificate = new X509Certificate2($"{pathToCerts}testdevice02.pfx", "1234");
            X509Certificate2 enrollmentCertificate = new X509Certificate2($"{pathToCerts}dpsIntermediate1.pfx", "1234");
            await dpsRegisterDevice.RegisterDeviceAsync(deviceCertificate, enrollmentCertificate);

            
            var ioTHubUpdateDevice = sp.GetService<IoTHubUpdateDevice>();
            // DISABLE Device iot Hub
            await ioTHubUpdateDevice.DisableDeviceAsync("testdevice02");
            // ENABLE Device iot Hub
            await ioTHubUpdateDevice.EnableDeviceAsync("testdevice02");

            // DISABLE DPS Device
            var dpsUpdateDevice = sp.GetService<DpsUpdateDevice>();
            //await dpsUpdateDevice.DisableDevice("testdevice02");
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
