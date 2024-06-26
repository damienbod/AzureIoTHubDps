using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DpsWebManagement.Pages.DpsCerts;

public class DpsCertificatesModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    public List<CertificatesModel> Certificates { get; set; } = new();

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
        }).ToList();
    }
}

public class CertificatesModel
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
