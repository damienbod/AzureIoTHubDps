using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

        public async Task DisableEnrollmentGroupAsync(string deviceId)
        {
            var queryResult = await _provisioningServiceClient
                .CreateEnrollmentGroupQuery(new QuerySpecification("*")).NextAsync();

        }

        public async Task EnableEnrollmentGroupAsync(string deviceId)
        {
            var queryResult = await _provisioningServiceClient
                .CreateEnrollmentGroupQuery(new QuerySpecification("*")).NextAsync();
        }

        public async Task DisableDeviceAsync(string deviceId)
        {
            var queryResult = await _provisioningServiceClient
                .CreateEnrollmentGroupQuery(new QuerySpecification("*")).NextAsync();

        }

        public async Task EnableDeviceAsync(string deviceId)
        {
            var queryResult = await _provisioningServiceClient
                .CreateEnrollmentGroupQuery(new QuerySpecification("*")).NextAsync();
        }
    }
}
