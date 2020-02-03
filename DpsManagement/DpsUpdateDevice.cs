using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;

namespace DpsManagement
{
    public class DpsUpdateDevice
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;

        public DpsUpdateDevice(
          IConfiguration configuration)
        {
            _provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(configuration.GetConnectionString("DpsConnection"));
        }


        public void DisableDevice(string deviceId)
        {
            //_provisioningServiceClient.
        }

        public void EnableDevice(string deviceId)
        {

        }
    }
}
