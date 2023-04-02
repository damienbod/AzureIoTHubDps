using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace DpsWebManagement.Pages.DpsCerts;

public class DownloadDpsCertificateModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public DownloadDpsCertificateModel(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    public async Task<ActionResult> OnGetAsync(int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(id);
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey),
            "application/octet-stream",
            $"{cert.Name}.pem");
    }
}
