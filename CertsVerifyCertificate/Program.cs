using CertificateManager;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace CertsVerifyCertificate;

/// <summary>
/// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-x509-get-started
/// </summary>
class Program
{
    static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
    static readonly string _pathToCerts = $"{_directory}/../../../../Certs/";

    static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddCertificateManager()
            .BuildServiceProvider();

        var createClientServerAuthCerts = serviceProvider.GetService<CreateCertificatesClientServerAuth>();
        var importExportCertificate = serviceProvider.GetService<ImportExportCertificate>();
        if (createClientServerAuthCerts == null) throw new ArgumentNullException(nameof(createClientServerAuthCerts));
        if (importExportCertificate == null) throw new ArgumentNullException(nameof(importExportCertificate));

        var root = new X509Certificate2($"{_pathToCerts}dpsCa.pfx", "1234");

        var verify = createClientServerAuthCerts.NewDeviceVerificationCertificate(
        "5FF0630C6EE1BADB9D1C783271051D3963B896B1C5753A9C", root);
        //verify.FriendlyName = "verify";

        var verifyPEM = importExportCertificate.PemExportPublicKeyCertificate(verify);
        File.WriteAllText("verify.pem", verifyPEM);

        Console.WriteLine("Certificates exported to pfx and cer files");
    }
}