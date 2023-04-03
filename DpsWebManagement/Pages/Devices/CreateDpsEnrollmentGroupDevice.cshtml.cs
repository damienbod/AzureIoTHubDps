using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DpsWebManagement.Pages.Devices;

public class CreateDpsEnrollmentGroupDeviceModel : PageModel
{
    private readonly DpsEnrollmentGroupProvider _dpsEnrollmentGroup;
    private readonly DpsRegisterDeviceProvider _dpsRegisterDeviceProvider;

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string DpsEnrollmentGroup { get; set; } = string.Empty;

    public List<SelectListItem> DpsEnrollmentGroups { get; set; } = new List<SelectListItem>();

    public CreateDpsEnrollmentGroupDeviceModel(DpsRegisterDeviceProvider dpsRegisterDeviceProvider,
        DpsEnrollmentGroupProvider dpsEnrollmentGroup)
    {
        _dpsEnrollmentGroup = dpsEnrollmentGroup;
        _dpsRegisterDeviceProvider = dpsRegisterDeviceProvider;
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

        await _dpsRegisterDeviceProvider.RegisterDeviceAsync(Name, DpsEnrollmentGroup);
        Message = $"{Name}";

        await GetSelectItems();
        return await OnGetAsync();
    }

    private async Task GetSelectItems()
    {
        var result = await _dpsEnrollmentGroup.GetDpsGroupsAsync();
        DpsEnrollmentGroups = result.Select(a =>
            new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name
            }).ToList();
    }
}
