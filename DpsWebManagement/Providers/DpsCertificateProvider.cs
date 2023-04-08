using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DpsWebManagement.Providers;

public class DpsCertificateProvider
{
    private readonly CreateCertificatesClientServerAuth _createCertificatesClientServerAuth;
    private readonly ImportExportCertificate _iec;
    private readonly DpsDbContext _dpsDbContext;

    public DpsCertificateProvider(CreateCertificatesClientServerAuth createCertificatesClientServerAuth,
        ImportExportCertificate importExportCertificate,
        DpsDbContext dpsDbContext)
    {
        _createCertificatesClientServerAuth = createCertificatesClientServerAuth;
        _iec = importExportCertificate;
        _dpsDbContext = dpsDbContext;
    }

    public async Task<(string PublicPem, int Id)> CreateCertificateForDpsAsync(string certName)
    {
        var certificateDps = _createCertificatesClientServerAuth.NewRootCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            3, certName);

        var publicKeyPem = _iec.PemExportPublicKeyCertificate(certificateDps);

        string pemPrivateKey = string.Empty;
        using (ECDsa? ecdsa = certificateDps.GetECDsaPrivateKey())
        {
            pemPrivateKey = ecdsa!.ExportECPrivateKeyPem();
            FileProvider.WriteToDisk($"{certName}-private.pem", pemPrivateKey);
        }
 
        var item = new DpsCertificate
        {
            Name = certName,
            PemPrivateKey = pemPrivateKey,
            PemPublicKey = publicKeyPem
        };

        _dpsDbContext.DpsCertificates.Add(item);

        await _dpsDbContext.SaveChangesAsync();

        return (publicKeyPem, item.Id);
    }

    public async Task<List<DpsCertificate>> GetDpsCertificatesAsync()
    {
        return await _dpsDbContext.DpsCertificates.ToListAsync();
    }

    public async Task<DpsCertificate?> GetDpsCertificateAsync(int id)
    {
        return await _dpsDbContext.DpsCertificates.FirstOrDefaultAsync(item => item.Id == id);
    }
}
