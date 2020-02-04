using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class DpsIndividualEnrollment
    {
        private IConfiguration Configuration { get; set; }

        private readonly ILogger<DpsIndividualEnrollment> _logger;

        public DpsIndividualEnrollment(IConfiguration config, ILoggerFactory loggerFactory)
        {
            Configuration = config;
            _logger = loggerFactory.CreateLogger<DpsIndividualEnrollment>();
        }

        public async Task CreateIndividualEnrollment(
            X509Certificate2 enrollmentCertificate, 
            string individualEnrollmentId)
        {
            _logger.LogInformation("Starting CreateIndividualEnrollment...");

            using (ProvisioningServiceClient provisioningServiceClient =
                   ProvisioningServiceClient.CreateFromConnectionString(
                       Configuration.GetConnectionString("DpsConnection")))
            {

                _logger.LogInformation("Creating a new individualEnrollment...");

                try
                {
                    Attestation attestation = X509Attestation.CreateFromClientCertificates(enrollmentCertificate);
                    //IndividualEnrollment individualEnrollment =
                    //    new IndividualEnrollment(
                    //            IndividualEnrollmentId,
                    //            attestation);

                    IndividualEnrollment individualEnrollment =
                            new IndividualEnrollment(individualEnrollmentId, attestation)
                            {
                                ProvisioningStatus = ProvisioningStatus.Enabled,
                                DeviceId = "testdevice02",
                                Capabilities = new Microsoft.Azure.Devices.Shared.DeviceCapabilities
                                {
                                    IotEdge = true
                                },
                                InitialTwinState = new TwinState(
                                   new Microsoft.Azure.Devices.Shared.TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + TimeZoneInfo.Local.DisplayName + "\" }"),
                                   new Microsoft.Azure.Devices.Shared.TwinCollection("{}"))
                            };

                    _logger.LogInformation($"{individualEnrollment}");
              
                    individualEnrollment.ReprovisionPolicy = new ReprovisionPolicy();
                    individualEnrollment.ReprovisionPolicy.MigrateDeviceData = false;
                    individualEnrollment.ReprovisionPolicy.UpdateHubAssignment = true;

                    _logger.LogInformation("Adding new individualEnrollment...");

                    var individualEnrollmentResult =
                        await provisioningServiceClient
                            .CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment)
                            .ConfigureAwait(false);
                    _logger.LogInformation("EnrollmentGroup created with success.");
                    _logger.LogInformation($"{individualEnrollmentResult}");
                }
                catch (Exception e)
                {

                }


            }
        }
    }
}
