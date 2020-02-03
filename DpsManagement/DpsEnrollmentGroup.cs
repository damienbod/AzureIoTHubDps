using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class DpsEnrollmentGroup
    {
        private IConfiguration Configuration { get;set;}

        private readonly ILogger<DpsEnrollmentGroup> _logger;

        public DpsEnrollmentGroup(IConfiguration config, ILoggerFactory loggerFactory)
        {
            Configuration = config;
            _logger = loggerFactory.CreateLogger<DpsEnrollmentGroup>();
        }
        
        public async Task CreateDpsEnrollmentGroupAsync(
            string enrollmentGroupId, 
            X509Certificate2 pemCertificate)
        {
            _logger.LogInformation("Starting CreateDpsEnrollmentGroupAsync...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(
                        Configuration.GetConnectionString("DpsConnection")))
            {
                _logger.LogInformation("Creating a new enrollmentGroup...");
                var certificate = new X509Certificate2(pemCertificate);

                Attestation attestation = X509Attestation.CreateFromRootCertificates(certificate);
                EnrollmentGroup enrollmentGroup = new EnrollmentGroup(enrollmentGroupId, attestation)
                {
                    ProvisioningStatus = ProvisioningStatus.Enabled,
                    ReprovisionPolicy = new ReprovisionPolicy
                    {
                        MigrateDeviceData = false,
                        UpdateHubAssignment = true
                    },
                    Capabilities = new DeviceCapabilities
                    {
                        IotEdge = false
                    },
                    InitialTwinState = new TwinState(
                        new TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + TimeZoneInfo.Local.DisplayName + "\" }"),
                        new TwinCollection("{ \"authenticationType\": \"certificateAuthority\"}")
                    )
                };
                _logger.LogInformation($"{enrollmentGroup}");
                _logger.LogInformation($"Adding new enrollmentGroup...");

                EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient
                    .CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup)
                    .ConfigureAwait(false);

                _logger.LogInformation($"EnrollmentGroup created with success.");
                _logger.LogInformation($"{enrollmentGroupResult}");
            }
        }
    }
}
