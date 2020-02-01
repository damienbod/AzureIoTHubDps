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

            var root = new X509Certificate2("dpsIntermediate2.pfx", "1234");

            var verify = createClientServerAuthCerts.NewDeviceVerificationCertificate(
            "B58A527D3B559B9EA1F23AFEEE719F7F6D034E0A97EAB260", root);
            verify.FriendlyName = "verify";

            var verifyPEM = importExportCertificate.PemExportPublicKeyCertificate(verify);
            File.WriteAllText("verify.pem", verifyPEM);

            Console.WriteLine("Certificates exported to pfx and cer files");
        }
    }
}