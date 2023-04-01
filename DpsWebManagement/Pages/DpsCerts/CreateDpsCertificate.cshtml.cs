using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DpsWebManagement.Pages.DpsCerts;

public class CreateDpsCertificateModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    public CreateDpsCertificateModel(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return OnGet();
        }

        var publicKeyPem = await _dpsCertificateProvider.CreateCertificateForDpsAsync(Name);

        Message = $"{publicKeyPem}";

        return OnGet();
    }
}
