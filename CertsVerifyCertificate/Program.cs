using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace CertsVerifyCertificate
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

            var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();

            var root = new X509Certificate2("dpsCa.pfx", "1234");

            var verify = createClientServerAuthCerts.NewDeviceVerificationCertificate(
            "F61FA95B0A5792E3839C4364079A67E590D3FEB636BEFAA5", root);
            verify.FriendlyName = "verify";

            var verifyPEM = importExportCertificate.PemExportPublicKeyCertificate(verify);
            File.WriteAllText("verify.pem", verifyPEM);

            Console.WriteLine("Certificates exported to pfx and cer files");
        }
    }
}