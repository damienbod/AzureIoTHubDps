using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DpsWebManagement.Controllers;

[Authorize]
public class FileDownloadController : Controller
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    public FileDownloadController(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    public async Task<IActionResult> DownloadFileAsync(int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(2);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}.pem");
    }
}
