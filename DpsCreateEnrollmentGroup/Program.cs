using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace DpsCreateEnrollmentGroup
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/iot-dps/quick-enroll-device-x509-csharp
    /// </summary>
    class Program
    {
        private static string ProvisioningConnectionString = "HostName=damienbod.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=qaOUyWmuUaC3w/0FC2Hfe+yHGBpe7hHjrwQl7f+vY3M=";
        private static string EnrollmentGroupId = "dpsIntermediate2";
        private static string X509RootCertPath = @"dpsIntermediate2.pem";

        static void Main(string[] args)
        {
            RunSample().GetAwaiter().GetResult();
            Console.WriteLine("\nHit <Enter> to exit ...");
            Console.ReadLine();
        }

        public static async Task RunSample()
        {
            Console.WriteLine("Starting sample...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(ProvisioningConnectionString))
            {
                #region Create a new enrollmentGroup config
                Console.WriteLine("\nCreating a new enrollmentGroup...");
                var certificate = new X509Certificate2(X509RootCertPath);


                Attestation attestation = X509Attestation.CreateFromRootCertificates(certificate);
                EnrollmentGroup enrollmentGroup =
                        new EnrollmentGroup(
                                EnrollmentGroupId,
                                attestation)
                        {
                            ProvisioningStatus = ProvisioningStatus.Enabled
                        };
                Console.WriteLine(enrollmentGroup);
                #endregion


                enrollmentGroup.ReprovisionPolicy = new ReprovisionPolicy();
                enrollmentGroup.ReprovisionPolicy.MigrateDeviceData = false;
                enrollmentGroup.ReprovisionPolicy.UpdateHubAssignment = true;

                var timeZoneItem = TimeZoneInfo.Local;

                enrollmentGroup.InitialTwinState = new TwinState(
                   new Microsoft.Azure.Devices.Shared.TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + timeZoneItem.DisplayName + "\" }"),
                   new Microsoft.Azure.Devices.Shared.TwinCollection("{}")
               );
                #region Create the enrollmentGroup
                Console.WriteLine("\nAdding new enrollmentGroup...");
                EnrollmentGroup enrollmentGroupResult =
                    await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
                Console.WriteLine("\nEnrollmentGroup created with success.");
                Console.WriteLine(enrollmentGroupResult);

                
                #endregion

            }
        }
    }
}
