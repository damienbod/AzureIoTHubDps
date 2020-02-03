using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class DpsEnrollmentGroup
    {
        private static string ProvisioningConnectionString = "HostName=damienbod.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=qaOUyWmuUaC3w/0FC2Hfe+yHGBpe7hHjrwQl7f+vY3M=";
        private static string EnrollmentGroupId = "dpsIntermediate2";
        private static string X509RootCertPath = @"dpsIntermediate2.pem";

        public async Task CreateDpsEnrollmentGroupAsync()
        {
            Console.WriteLine("Starting CreateDpsEnrollmentGroupAsync...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(ProvisioningConnectionString))
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
