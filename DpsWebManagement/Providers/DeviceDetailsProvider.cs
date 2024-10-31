using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.EntityFrameworkCore;

namespace DpsWebManagement.Providers;

public class DeviceDetailsProvider
{
    private IConfiguration Configuration { get; set; }
    private readonly ILogger<DpsRegisterDeviceProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;

    private readonly ProvisioningServiceClient _provisioningServiceClient;

    public DeviceDetailsProvider(IConfiguration config,
        ILoggerFactory loggerFactory, DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDeviceProvider>();
        _dpsDbContext = dpsDbContext;

        _provisioningServiceClient = ProvisioningServiceClient
              .CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection"));
    }

    public async Task<DeviceRegistrationState?> GetAzureDpsDeviceRegistrationState(string? deviceId)
    {
        var device = await _provisioningServiceClient
            .GetDeviceRegistrationStateAsync(deviceId);

        return device;
    }

    public async Task<Device?> GetAzureIoTDevice(string? deviceId, string assignedIotHub)
    {
        var registryManager = RegistryManager.CreateFromConnectionString(
          Configuration.GetConnectionString(assignedIotHub));

        var device = await registryManager.GetDeviceAsync(deviceId);

        return device;
    }

    public async Task<Device?> DisableIoTDeviceAsync(string deviceId, string assignedIotHub)
    {
        var registryManager = RegistryManager.CreateFromConnectionString(
          Configuration.GetConnectionString(assignedIotHub));

        var device = await registryManager.GetDeviceAsync(deviceId);
        device.Status = DeviceStatus.Disabled;
        device = await registryManager.UpdateDeviceAsync(device);

        _logger.LogInformation("iot hub device disabled  {device}", device);

        return device;
    }

    public async Task<Device?> EnableIoTDeviceAsync(string deviceId, string assignedIotHub)
    {
        var registryManager = RegistryManager.CreateFromConnectionString(
          Configuration.GetConnectionString(assignedIotHub));

        var device = await registryManager.GetDeviceAsync(deviceId);
        device.Status = DeviceStatus.Enabled;
        device = await registryManager.UpdateDeviceAsync(device);
        _logger.LogInformation("iot hub device enabled  {device}", device);

        return device;
    }

    public async Task UpdateAuthDeviceCertificateAuthorityAsync(string deviceId,
        string thumbprint, string assignedIotHub)
    {
        var registryManager = RegistryManager.CreateFromConnectionString(
          Configuration.GetConnectionString(assignedIotHub));

        var device = await registryManager.GetDeviceAsync(deviceId);
        device.Authentication = new AuthenticationMechanism
        {
            X509Thumbprint = new X509Thumbprint
            {
                PrimaryThumbprint = thumbprint
            },
            Type = AuthenticationType.CertificateAuthority
        };
        device = await registryManager.UpdateDeviceAsync(device);

        _logger.LogInformation("iot hub device updated  {device}", device);
    }

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceFromDbAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .Include(device => device.DpsEnrollmentGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
