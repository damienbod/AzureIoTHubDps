using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

namespace DpsWebManagement.Pages.DpsCerts;

public class DpsCertificatesModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    public List<CertificatesModel> Certificates { get; set; } = new();

    [BindProperty]
    public int? Id { get; set; }

    public DpsCertificatesModel(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    public async Task OnGetAsync()
    {
        var data = await _dpsCertificateProvider.GetDpsCertificatesAsync();
        Certificates = data.Select(item => new CertificatesModel
        {
            Id = item.Id,
            Name = item.Name,
            DownloadLink = $"/DownloadFile/{item.Id}"
        }).ToList();
    }

    public async Task<ActionResult> OnPostDownloadFile(int id)
    {
        var cert = await _dpsCertificateProvider.GetDpsCertificateAsync(id);
        if(cert == null) throw new ArgumentNullException(nameof(cert));
        if (cert.PemPublicKey == null) throw new ArgumentNullException(nameof(cert.PemPublicKey));

        return File(Encoding.UTF8.GetBytes(cert.PemPublicKey), 
            "application/octet-stream",
            $"{cert.Name}.pem");
    }
}

public class CertificatesModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? DownloadLink { get; set; }
}
