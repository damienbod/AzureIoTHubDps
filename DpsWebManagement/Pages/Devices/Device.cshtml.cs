using DpsWebManagement.Providers;
using DpsWebManagement.Providers.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Devices;

namespace DpsWebManagement.Pages.Devices;

public class DeviceModel : PageModel
{
    private readonly DeviceDetailsProvider _deviceDetailsProvider;

    [BindProperty]
    public int? Id { get; set; }

    [BindProperty]
    public string? DeviceId { get; set; }

    [BindProperty]
    public string? AssignedHub { get; set; }

    public DpsDeviceData DpsDevice = new();

    public DeviceModel(DeviceDetailsProvider deviceDetailsProvider)
    {
        _deviceDetailsProvider = deviceDetailsProvider;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var data = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(id);

        if (data == null)
        {
            return NotFound();
        }

        await InitializeModel(data);

        return Page();
    }

    private async Task InitializeModel(DpsEnrollmentDevice? data)
    {
        DpsDevice = new DpsDeviceData
        {
            Id = data!.Id,
            Name = data.Name,
            Password = data.Password,
            DpsEnrollmentGroup = data.DpsEnrollmentGroup.Name,
            AssignedHub = data.AssignedHub,
            RegistrationId = data.RegistrationId,
            DeviceId = data.DeviceId
        };

        DeviceId = data.DeviceId;
        AssignedHub = data.AssignedHub;
        Id = data.Id;

        if (data.AssignedHub != null)
        {
            var azureIotDevice = await _deviceDetailsProvider
                .GetAzureIoTDevice(data.DeviceId, data.AssignedHub!);

            DpsDevice.Enabled = (azureIotDevice!.Status == DeviceStatus.Enabled);
            DpsDevice.ConnectionState = azureIotDevice.ConnectionState;
            DpsDevice.LastActivityTime = azureIotDevice.LastActivityTime;
        }
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        if (!ModelState.IsValid)
        {
            var data = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(Id!.Value);
            await InitializeModel(data);
            return Page();
        }

        var result = await _deviceDetailsProvider
            .DisableIoTDeviceAsync(DeviceId, AssignedHub);

        var newData = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(Id!.Value);
        await InitializeModel(newData);
        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        if (!ModelState.IsValid)
        {
            var data = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(Id!.Value);
            await InitializeModel(data);
            return Page();
        }

        var result = await _deviceDetailsProvider
            .EnableIoTDeviceAsync(DeviceId, AssignedHub);

        var newData = await _deviceDetailsProvider.GetDpsDeviceFromDbAsync(Id!.Value);
        await InitializeModel(newData);
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
    public DeviceConnectionState ConnectionState { get; set; }
    public DateTime LastActivityTime { get; internal set; }
}