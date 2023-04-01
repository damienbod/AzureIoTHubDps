using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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

    public async Task<string> CreateCertificateForDpsAsync(string certName)
    {
        var password = GetEncodedRandomString(30);
        var dpsCertificate = _createCertificatesClientServerAuth.NewRootCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            3, certName);

        dpsCertificate.FriendlyName = "DPS group root certificate";

        var publicKeyPem = _importExportCertificate.PemExportPublicKeyCertificate(dpsCertificate);
        var privateKeyPem = _importExportCertificate.PemExportPfxFullCertificate(dpsCertificate, password);

        //var rootCertInPfxBtyes = _importExportCertificate.ExportRootPfx(password, dpsCertificate);
        //File.WriteAllBytes($"{certName}.pfx", rootCertInPfxBtyes);

        //var dpsCaPEM = _importExportCertificate.PemExportPublicKeyCertificate(dpsCertificate);
        //File.WriteAllText($"{certName}.pem", dpsCaPEM);

        _dpsDbContext.DpsCertificates.Add(new Model.DpsCertificate
        {
            Name = certName,
            PemPrivateKey = privateKeyPem,
            PemPublicKey = publicKeyPem,
            Password = password
        });

        await _dpsDbContext.SaveChangesAsync();

        return publicKeyPem;
    }

    public async Task<List<DpsCertificate>> GetDpsCertificatesAsync()
    {
        return await _dpsDbContext.DpsCertificates.ToListAsync();
    }

    public async Task<DpsCertificate?> GetDpsCertificatesAsync(int id)
    {
        return await _dpsDbContext.DpsCertificates.FirstOrDefaultAsync(item => item.Id == id);
    }

    private string GetEncodedRandomString(int length)
    {
        var base64 = Convert.ToBase64String(GenerateRandomBytes(length));
        return base64;
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var byteArray = new byte[length];
        RandomNumberGenerator.Fill(byteArray);
        return byteArray;
    }
}
