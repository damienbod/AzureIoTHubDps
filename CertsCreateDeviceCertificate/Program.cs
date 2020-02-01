using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace CertsCreateDeviceCertificate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-x509-get-started
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddCertificateManager()
                .BuildServiceProvider();

            var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();

            var intermediate = new X509Certificate2("dpsIntermediate2.pfx", "1234");

            var device = createClientServerAuthCerts.NewDeviceChainedCertificate(
                new DistinguishedName { CommonName = "testdevice02" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                "testdevice02", intermediate);
            device.FriendlyName = "IoT device testdevice02";
      
            string password = "1234";
            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();

            var deviceInPfxBytes = importExportCertificate.ExportChainedCertificatePfx(password, device, intermediate);
            File.WriteAllBytes("testdevice02.pfx", deviceInPfxBytes);
    
        }
    }
}
