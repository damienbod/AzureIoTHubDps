using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DpsWebManagement.Pages.Devices;

public class DeviceModel : PageModel
{
    private readonly DpsRegisterDeviceProvider _dpsRegisterDeviceProvider;

    public DpsDeviceData DpsDevice = new DpsDeviceData();
    public DeviceModel(DpsRegisterDeviceProvider dpsRegisterDeviceProvider)
    {
        _dpsRegisterDeviceProvider = dpsRegisterDeviceProvider;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var data = await _dpsRegisterDeviceProvider.GetDpsDeviceAsync(id);
        if(data == null)
        {
            return NotFound();
        }

        DpsDevice = new DpsDeviceData
        {
            Id = data.Id,
            Name = data.Name,
            Password = data.Password,
            DpsEnrollmentGroup = data.DpsEnrollmentGroupId
        };
        return Page();
    }
}

public class DpsDeviceData
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Password { get; set; }  

    public int? DpsEnrollmentGroup { get; set; }
}