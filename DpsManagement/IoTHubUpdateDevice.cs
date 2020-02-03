using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class IoTHubUpdateDevice
    {
        private readonly RegistryManager _registryManager;
        private IConfiguration Configuration { get; set; }

        private readonly ILogger<IoTHubUpdateDevice> _logger;
   
        public IoTHubUpdateDevice(IConfiguration config, ILoggerFactory loggerFactory)
        {
            Configuration = config;
            _logger = loggerFactory.CreateLogger<IoTHubUpdateDevice>();
            _registryManager = RegistryManager.CreateFromConnectionString(
                Configuration.GetConnectionString("IoTHubConnection"));
        }

        public async Task DisableDeviceAsync(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            device.Status = DeviceStatus.Disabled;
            device = await _registryManager.UpdateDeviceAsync(device);
            _logger.LogInformation($"iot hub device disabled  {device}");
        }

        public async Task EnableDeviceAsync(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            device.Status = DeviceStatus.Enabled;
            device = await _registryManager.UpdateDeviceAsync(device);
            _logger.LogInformation($"iot hub device enabled  {device}");
        }

        //public async Task quuu(string deviceId)
        //{
        //    //var devices = await _registryManager.CreateQuery("SELECT * FROM devices WHERE status = 'disabled'").GetNextAsJsonAsync();

        //    //foreach (var devicestring in devices)
        //    //{
        //    //    var device = JsonConvert.DeserializeObject<Device>(devicestring);

        //    //    if (!device.Id.EndsWith(".edge"))
        //    //    {
        //    //        // dod 
        //    //    }
        //    //}
        //}
    }
}
