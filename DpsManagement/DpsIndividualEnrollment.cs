using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace DpsManagement;

public class DpsIndividualEnrollment
{
    private IConfiguration Configuration { get; set; }

    private readonly ILogger<DpsIndividualEnrollment> _logger;

    public DpsIndividualEnrollment(IConfiguration config, ILoggerFactory loggerFactory)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsIndividualEnrollment>();
    }

    public async Task CreateIndividualEnrollment(string individualEnrollmentId, X509Certificate2 enrollmentCertificate)
    {
        _logger.LogInformation("Starting CreateIndividualEnrollment...");

        using (ProvisioningServiceClient provisioningServiceClient =
               ProvisioningServiceClient.CreateFromConnectionString(
                   Configuration.GetConnectionString("DpsConnection")))
        {
            _logger.LogInformation("Creating a new individualEnrollment...");

            Attestation attestation = X509Attestation.CreateFromClientCertificates(enrollmentCertificate);
            //IndividualEnrollment individualEnrollment =
            //    new IndividualEnrollment(
            //            IndividualEnrollmentId,
            //            attestation);

            var individualEnrollment = new IndividualEnrollment(individualEnrollmentId, attestation)
            {
                ProvisioningStatus = ProvisioningStatus.Enabled,
                DeviceId = individualEnrollmentId,
                Capabilities = new Microsoft.Azure.Devices.Shared.DeviceCapabilities
                {
                    IotEdge = false
                },
                InitialTwinState = new TwinState(
                    new Microsoft.Azure.Devices.Shared.TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + TimeZoneInfo.Local.DisplayName + "\" }"),
                    new Microsoft.Azure.Devices.Shared.TwinCollection("{}")
                ),
                ReprovisionPolicy = new ReprovisionPolicy
                {
                    MigrateDeviceData = false,
                    UpdateHubAssignment = true
                }
            };

            _logger.LogInformation("{individualEnrollment}", individualEnrollment);
            _logger.LogInformation("Adding new individualEnrollment...");

            //var deviceStatus = await provisioningServiceClient.GetDeviceRegistrationStateAsync(individualEnrollmentId);
            //var deviceEnrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(individualEnrollmentId);

            var individualEnrollmentResult =
                await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);

            _logger.LogInformation("individualEnrollment created with success.");
            _logger.LogInformation("{individualEnrollmentResult}", individualEnrollmentResult);
        }
    }
}
