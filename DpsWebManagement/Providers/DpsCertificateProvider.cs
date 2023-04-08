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
    private readonly ImportExportCertificate _importExportCertificate;
    private readonly DpsDbContext _dpsDbContext;

    public DpsCertificateProvider(CreateCertificatesClientServerAuth createCertificatesClientServerAuth,
        ImportExportCertificate importExportCertificate,
        DpsDbContext dpsDbContext)
    {
        _createCertificatesClientServerAuth = createCertificatesClientServerAuth;
        _importExportCertificate = importExportCertificate;
        _dpsDbContext = dpsDbContext;
    }

    public async Task<(string PublicPem, int Id)> CreateCertificateForDpsAsync(string certName)
    {
        var dpsCertificate = _createCertificatesClientServerAuth.NewRootCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            3, certName);
        //dpsCertificate.FriendlyName = "DPS group root certificate";

        var publicKeyPem = _importExportCertificate.PemExportPublicKeyCertificate(dpsCertificate);

        string privateKeyPem = string.Empty;
        using (ECDsa? ecdsa = dpsCertificate.GetECDsaPrivateKey())
        {
            privateKeyPem = ecdsa!.ExportECPrivateKeyPem();
            FileProvider.WriteToDisk($"{certName}-private.pem", privateKeyPem);
        }
 
        var item = new DpsCertificate
        {
            Name = certName,
            PemPrivateKey = privateKeyPem,
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
