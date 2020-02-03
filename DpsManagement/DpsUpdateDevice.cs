using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DpsManagement
{
    public class DpsUpdateDevice
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;

        private IConfiguration Configuration { get; set; }
        private readonly ILogger<DpsUpdateDevice> _logger;

        public DpsUpdateDevice(IConfiguration config, ILoggerFactory loggerFactory)
        {
            Configuration = config;
            _logger = loggerFactory.CreateLogger<DpsUpdateDevice>();

            _provisioningServiceClient = ProvisioningServiceClient
                .CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection"));
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
