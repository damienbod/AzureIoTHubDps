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
    public string? Message { get; set; } = null;

    [BindProperty]
    public int? DeviceId { get; set; }

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
        var group = await _dpsEnrollmentGroup.GetDpsGroupAsync(int.Parse(DpsEnrollmentGroup));
        var isNameValid = Name != null && Name.StartsWith(group!.Name);
        //if (!isNameValid)
        //{
        //    ModelState.AddModelError("Name", $"The device name should start with the group name: {group!.Name}");
        //}

        if (!ModelState.IsValid)
        {
            await GetSelectItems();
            return await OnGetAsync();
        }

        var result = await _dpsRegisterDeviceProvider.RegisterDeviceAsync(Name!, DpsEnrollmentGroup);
        
        if(result.ErrorMessage!= null) Message = $"{result.ErrorMessage}";
        DeviceId  = result.DeviceId;

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
