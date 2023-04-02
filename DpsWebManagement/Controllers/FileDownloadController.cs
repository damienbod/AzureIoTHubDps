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

    public FileDownloadController(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    [HttpPost("DpsCertificatePem")]
    public async Task<IActionResult> DpsCertificatePemAsync(int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}.pem");
    }
}
