using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DpsWebManagement.Pages.Groups;

public class CreateDpsEnrollmentGroupModel : PageModel
{
    private readonly DpsCertificateProvider _dpsCertificateProvider;
    private readonly DpsEnrollmentGroupProvider _dpsEnrollmentGroup;

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string DpsCertificate { get; set; } = string.Empty;

    public List<SelectListItem> DpsCertificates { get; set; } = new List<SelectListItem>();

    public CreateDpsEnrollmentGroupModel(DpsCertificateProvider dpsCertificateProvider,
        DpsEnrollmentGroupProvider dpsEnrollmentGroup)
    {
        _dpsCertificateProvider = dpsCertificateProvider;
        _dpsEnrollmentGroup = dpsEnrollmentGroup;
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

        var result = await _dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync(Name, DpsCertificate);
        Message = $"{result.Name}";

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
