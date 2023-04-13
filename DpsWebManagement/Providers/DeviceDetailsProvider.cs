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
    private readonly RegistryManager _registryManager;

    public DeviceDetailsProvider(IConfiguration config, 
        ILoggerFactory loggerFactory, DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDeviceProvider>();
        _dpsDbContext = dpsDbContext;

        _provisioningServiceClient = ProvisioningServiceClient
              .CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection"));

        _registryManager = RegistryManager.CreateFromConnectionString(
            Configuration.GetConnectionString("IoTHubConnection"));
    }

    public async Task<DeviceRegistrationState?> GetAzureDeviceRegistrationState(string? deviceId)
    {
        var device = await _provisioningServiceClient
            .GetDeviceRegistrationStateAsync(deviceId);

        return device;
    }

    public async Task DisableDeviceAsync(string deviceId)
    {
        var device = await _registryManager.GetDeviceAsync(deviceId);
        device.Status = DeviceStatus.Disabled;
        device = await _registryManager.UpdateDeviceAsync(device);
        _logger.LogInformation("iot hub device disabled  {device}", device);
    }

    public async Task EnableDeviceAsync(string deviceId)
    {
        var device = await _registryManager.GetDeviceAsync(deviceId);
        device.Status = DeviceStatus.Enabled;
        device = await _registryManager.UpdateDeviceAsync(device);
        _logger.LogInformation("iot hub device enabled  {device}", device);
    }

    public async Task UpdateAuthDeviceCertificateAuthorityAsync(string deviceId, string thumbprint)
    {
        var device = await _registryManager.GetDeviceAsync(deviceId);
        device.Authentication = new AuthenticationMechanism
        {
            X509Thumbprint = new X509Thumbprint
            {
                PrimaryThumbprint = thumbprint
            },
            Type = AuthenticationType.CertificateAuthority
        };
        device = await _registryManager.UpdateDeviceAsync(device);
        _logger.LogInformation("iot hub device updated  {device}", device);
    }

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .Include(device => device.DpsEnrollmentGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
