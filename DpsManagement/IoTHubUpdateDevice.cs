using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class IoTHubUpdateDevice
    {
        private readonly RegistryManager _registryManager;
        private IConfiguration Configuration { get; set; }

        public IoTHubUpdateDevice(IConfiguration config)
        {
            Configuration = config;
            _registryManager = RegistryManager.CreateFromConnectionString(
                Configuration.GetConnectionString("IoTHubConnection"));
        }

        public async Task DisableDeviceAsync(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            device.Status = DeviceStatus.Disabled;
            device = await _registryManager.UpdateDeviceAsync(device);
        }

        public async Task EnableDeviceAsync(string deviceId)
        {
            var device = await _registryManager.GetDeviceAsync(deviceId);
            device.Status = DeviceStatus.Enabled;
            device = await _registryManager.UpdateDeviceAsync(device);
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
