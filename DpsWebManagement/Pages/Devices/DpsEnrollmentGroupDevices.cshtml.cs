using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DpsWebManagement.Pages.Devices;

public class DpsEnrollmentGroupDevicesModel : PageModel
{
    private readonly DpsRegisterDeviceProvider _dpsRegisterDeviceProvider;

    public List<DpsDeviceModel> DpsDevices { get; set; } = new List<DpsDeviceModel>();

    public DpsEnrollmentGroupDevicesModel(DpsRegisterDeviceProvider dpsRegisterDeviceProvider)
    {
        _dpsRegisterDeviceProvider = dpsRegisterDeviceProvider;
    }

    public async Task OnGetAsync()
    {
        var data = await _dpsRegisterDeviceProvider.GetDpsDevicesAsync();
        DpsDevices = data.Select(item => new DpsDeviceModel
        {
            Id = item.Id,
            Name = item.Name,
            DpsEnrollmentGroup = item.DpsEnrollmentGroupId
        }).ToList();
    }
}

public class DpsDeviceModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? DpsEnrollmentGroup { get; set; }
}