using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace CertsCreateChained
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddCertificateManager()
                .BuildServiceProvider();

            string password = "1234";
            var cc = serviceProvider.GetService<CreateCertificatesClientServerAuth>();
            var iec = serviceProvider.GetService<ImportExportCertificate>();

            var dpsCa = cc.NewRootCertificate(
                new DistinguishedName { CommonName = "dpsCa", Country = "CH" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                3, "localhost");
            dpsCa.FriendlyName = "developement root certificate";

            var dpsIntermediate1 = cc.NewIntermediateChainedCertificate(
                new DistinguishedName { CommonName = "dpsIntermediate1", Country = "CH" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                2, "localhost", dpsCa);
            dpsIntermediate1.FriendlyName = "dpsIntermediate1 certificate";

            var dpsIntermediate2 = cc.NewIntermediateChainedCertificate(
                new DistinguishedName { CommonName = "dpsIntermediate2", Country = "CH" },
                new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
                2, "localhost", dpsCa);
            dpsIntermediate2.FriendlyName = "dpsIntermediate2 certificate";

            // EXPORTS PFX

            var rootCertInPfxBtyes = iec.ExportRootPfx(password, dpsCa);
            File.WriteAllBytes("dpsCa.pfx", rootCertInPfxBtyes);

            var dpsIntermediate1Btyes = iec.ExportChainedCertificatePfx(password, dpsIntermediate1, dpsCa);
            File.WriteAllBytes("dpsIntermediate1.pfx", dpsIntermediate1Btyes);

            var dpsIntermediate2Btyes = iec.ExportChainedCertificatePfx(password, dpsIntermediate2, dpsCa);
            File.WriteAllBytes("dpsIntermediate2.pfx", dpsIntermediate2Btyes);

            Console.WriteLine("Certificates exported to pfx and cer files");

            // EXPORTS PEM

            var dpsCaPEM = iec.PemExportPublicKeyCertificate(dpsCa);
            File.WriteAllText("dpsCa.pem", dpsCaPEM);

            var dpsIntermediate1PEM = iec.PemExportPublicKeyCertificate(dpsIntermediate1);
            File.WriteAllText("dpsIntermediate1.pem", dpsIntermediate1PEM);

            var dpsIntermediate2PEM = iec.PemExportPublicKeyCertificate(dpsIntermediate2);
            File.WriteAllText("dpsIntermediate2.pem", dpsIntermediate2PEM);
        }
    }
}
