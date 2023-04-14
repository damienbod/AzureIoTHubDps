using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Devices;

namespace DpsWebManagement.Pages.Devices;

public class DeviceModel : PageModel
{
    private readonly DeviceDetailsProvider _deviceDetailsProvider;

    [BindProperty]
    public int? Id { get; set; }

    public DpsDeviceData DpsDevice = new();

    public DeviceModel(DeviceDetailsProvider deviceDetailsProvider)
    {
        _deviceDetailsProvider = deviceDetailsProvider;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var data = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(id);

        var azureIotDevice = await _deviceDetailsProvider
            .GetAzureIoTDevice(data!.DeviceId, data.AssignedHub!);

        if (data == null)
        {
            return NotFound();
        }

        DpsDevice = new DpsDeviceData
        {
            Id = data.Id,
            Name = data.Name,
            Password = data.Password,
            DpsEnrollmentGroup = data.DpsEnrollmentGroup.Name,
            AssignedHub = data.AssignedHub,
            RegistrationId = data.RegistrationId,
            DeviceId = data.DeviceId,
            Enabled = (azureIotDevice!.Status == DeviceStatus.Enabled)
        };
        return Page();
    }
}

public class DpsDeviceData
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Password { get; set; }  

    public string? DpsEnrollmentGroup { get; set; }

    public string? AssignedHub { get; set; }
    public string? DeviceId { get; set; }
    public string? RegistrationId { get; set; }
    public bool Enabled { get; set; }
}