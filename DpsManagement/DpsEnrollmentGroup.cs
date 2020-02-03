using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class DpsEnrollmentGroup
    {
        private string EnrollmentGroupId = "dpsIntermediate2";
        private string X509RootCertPath = @"dpsIntermediate2.pem";

        private IConfiguration Configuration { get;set;}

        public DpsEnrollmentGroup(IConfiguration config)
        {
            Configuration = config;
        }
        
        public async Task CreateDpsEnrollmentGroupAsync()
        {
            Console.WriteLine("Starting CreateDpsEnrollmentGroupAsync...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection")))
            {
                Console.WriteLine("\nCreating a new enrollmentGroup...");
                var certificate = new X509Certificate2(X509RootCertPath);

                Attestation attestation = X509Attestation.CreateFromRootCertificates(certificate);
                EnrollmentGroup enrollmentGroup = new EnrollmentGroup(EnrollmentGroupId, attestation)
                {
                    ProvisioningStatus = ProvisioningStatus.Enabled
                };
                Console.WriteLine(enrollmentGroup);

                enrollmentGroup.ReprovisionPolicy = new ReprovisionPolicy
                {
                    MigrateDeviceData = false,
                    UpdateHubAssignment = true
                };

                enrollmentGroup.Capabilities = new DeviceCapabilities
                {
                    IotEdge = true
                };

                enrollmentGroup.InitialTwinState = new TwinState(
                    new TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + TimeZoneInfo.Local.DisplayName + "\" }"),
                    new TwinCollection("{\"authenticationType\": \"certificateAuthority\"}")
                );

                Console.WriteLine("\nAdding new enrollmentGroup...");

                EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient
                    .CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup)
                    .ConfigureAwait(false);

                Console.WriteLine("\nEnrollmentGroup created with success.");
                Console.WriteLine(enrollmentGroupResult);
            }
        }
    }
}
