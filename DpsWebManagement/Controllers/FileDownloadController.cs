using CertificateManager;
using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> DpsCertificatePemAsync([FromForm] int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}.pem");
    }

    [HttpPost("DpsDevicePfx")]
    public async Task<IActionResult> DpsDevicePfxAsync([FromForm] int id)
    {
        var cert = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PathToPfx == null) throw new ArgumentNullException(nameof(cert.PathToPfx));

        byte[] buff = System.IO.File.ReadAllBytes(cert.PathToPfx);
        return File(buff, "application/octet-stream",
            $"{cert.Name}.pfx");
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

    [HttpPost("DpsDevicePublicKeyPem")]
    public async Task<IActionResult> DpsDevicePublicKeyPemAsync([FromForm] int id)
    {
        var cert = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}-public.pem");
    }
}
