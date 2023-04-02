using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DpsWebManagement.Pages.Groups;

public class CreateDpsEnrollmentGroupModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string DpsCertificate { get; set; } = string.Empty;

    public List<SelectListItem> DpsCertificates { get; set; } = new List<SelectListItem>();

    public CreateDpsEnrollmentGroupModel(DpsCertificateProvider dpsCertificateProvider)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await GetSelectItems();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await GetSelectItems();
            return await OnGetAsync();
        }

        //Message = $"{result.PublicPem}";
        //Id = result.Id;

        await GetSelectItems();
        return await OnGetAsync();
    }

    private async Task GetSelectItems()
    {
        var result = await _dpsCertificateProvider.GetDpsCertificatesAsync();
        DpsCertificates = result.Select(a =>
            new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name
            }).ToList();
    }
}
