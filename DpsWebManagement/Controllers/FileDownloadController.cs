using CertificateManager;
using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace DpsWebManagement.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FileDownloadController : Controller
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;
    private readonly DpsRegisterDeviceProvider _dpsRegisterDeviceProvider;
    private readonly ImportExportCertificate _importExportCertificate;
    private readonly DpsEnrollmentGroupProvider _dpsEnrollmentGroupProvider;

    public FileDownloadController(DpsCertificateProvider dpsCertificateProvider,
        DpsRegisterDeviceProvider dpsRegisterDeviceProvider,
        ImportExportCertificate importExportCertificate,
        DpsEnrollmentGroupProvider dpsEnrollmentGroupProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
        _dpsRegisterDeviceProvider = dpsRegisterDeviceProvider;
        _importExportCertificate = importExportCertificate;
        _dpsEnrollmentGroupProvider = dpsEnrollmentGroupProvider;
    }

    [HttpPost("DpsCertificatePem")]
    public async Task<IActionResult> DpsCertificatePemAsync([FromForm]int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}.pem");
    }

    [HttpPost("DpsDevicePublicPem")]
    public async Task<IActionResult> DpsDevicePublicPemAsync([FromForm] int id)
    {
        var cert = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}-public.pem");
    }

    [HttpPost("DpsDevicePrivateKeyPem")]
    public async Task<IActionResult> DpsDevicePrivateKeyPemAsync([FromForm] int id)
    {
        var cert = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPrivateKey == null) throw new ArgumentNullException(nameof(cert.PemPrivateKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPrivateKey),
            "application/octet-stream",
            $"{cert.Name}-private.pem");
    }

    [HttpPost("DpsDevicePrivateKeyPfx")]
    public async Task<IActionResult> DpsDevicePrivateKeyPfxAsync([FromForm] int id)
    {
        var cert = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        var groupCert = await _dpsEnrollmentGroupProvider.GetDpsGroupAsync(cert.DpsEnrollmentGroupId);
        var rootCert = await _dpsCertificateProvider.GetDpsCertificateAsync(groupCert.DpsCertificateId);

        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPrivateKey == null) throw new ArgumentNullException(nameof(cert.PemPrivateKey));

        var xcert = _importExportCertificate.PemImportCertificate(cert.PemPrivateKey, cert.Password);
        var xgroupCert = _importExportCertificate.PemImportCertificate(groupCert.PemPrivateKey, groupCert.Password);
        var xrootCert = _importExportCertificate.PemImportCertificate(rootCert.PemPrivateKey, rootCert.Password);

        var rootInPfxBytes = _importExportCertificate.ExportRootPfx(rootCert.Password, xrootCert);
           
        var groupInPfxBytes = _importExportCertificate
            .ExportChainedCertificatePfx(groupCert.Password, xgroupCert, xrootCert);

        var deviceInPfxBytes = _importExportCertificate
            .ExportChainedCertificatePfx(groupCert.Password, xcert, xgroupCert);

        return File(deviceInPfxBytes, "application/octet-stream", $"{cert.Name}.pfx");
    }
}
