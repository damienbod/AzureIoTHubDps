using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace CertsCreateDeviceCertificate;

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
        var iec = serviceProvider.GetService<ImportExportCertificate>();
        if (createClientServerAuthCerts == null) throw new ArgumentNullException(nameof(createClientServerAuthCerts));
        if (iec == null) throw new ArgumentNullException(nameof(iec));

        var intermediate = new X509Certificate2($"{_pathToCerts}dpsIntermediate1.pfx", "1234");

        var device = createClientServerAuthCerts.NewDeviceChainedCertificate(
            new DistinguishedName { CommonName = "testdevice01" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
            "testdevice01", intermediate);
        device.FriendlyName = "IoT device testdevice01";
  
        string password = "1234";

        var deviceInPfxBytes = iec.ExportChainedCertificatePfx(password, device, intermediate);
        File.WriteAllBytes("testdevice01.pfx", deviceInPfxBytes);

        var devicePEM = iec.PemExportPublicKeyCertificate(device);
        File.WriteAllText("testdevice01.pem", devicePEM);
    }
}
