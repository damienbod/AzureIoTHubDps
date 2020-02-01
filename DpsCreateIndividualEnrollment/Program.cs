using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateManager;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DpsCreateEnrollmentGroup
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/iot-dps/quick-enroll-device-x509-csharp
    /// </summary>
    class Program
    {
        private static string ProvisioningConnectionString = "HostName=damienbod.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=qaOUyWmuUaC3w/0FC2Hfe+yHGBpe7hHjrwQl7f+vY3M=";
        private static string IndividualEnrollmentId = "testdevice01";
        private static string X509RootCertPath = @"testdevice01.pem";

        static void Main(string[] args)
        {
            //var serviceProvider = new ServiceCollection()
            //    .AddCertificateManager()
            //    .BuildServiceProvider();

            //var iec = serviceProvider.GetService<ImportExportCertificate>();

            //var testdevice01 = new X509Certificate2("testdevice01.pfx", "1234");
            //var testdevice02 = new X509Certificate2("testdevice02.pfx", "1234");
            //var testdevice01PEM = iec.PemExportPublicKeyCertificate(testdevice01);
            //File.WriteAllText("testdevice01.pem", testdevice01PEM);

            //var testdevice02PEM = iec.PemExportPublicKeyCertificate(testdevice02);
            //File.WriteAllText("testdevice02.pem", testdevice02PEM);

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
                Console.WriteLine("\nCreating a new individualEnrollment...");
                var certificate = new X509Certificate2(X509RootCertPath);


                try
                {
                    Attestation attestation = X509Attestation.CreateFromClientCertificates(certificate);
                    //IndividualEnrollment individualEnrollment =
                    //    new IndividualEnrollment(
                    //            IndividualEnrollmentId,
                    //            attestation);

                    IndividualEnrollment individualEnrollment =
                            new IndividualEnrollment(
                                    IndividualEnrollmentId,
                                    attestation)
                            {
                                ProvisioningStatus = ProvisioningStatus.Disabled
                            };

                    Console.WriteLine(individualEnrollment);
                    #endregion

                    individualEnrollment.ReprovisionPolicy = new ReprovisionPolicy();
                    individualEnrollment.ReprovisionPolicy.MigrateDeviceData = false;
                    individualEnrollment.ReprovisionPolicy.UpdateHubAssignment = true;

                    var timeZoneItem = TimeZoneInfo.Local;

                    individualEnrollment.InitialTwinState = new TwinState(
                       new Microsoft.Azure.Devices.Shared.TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + timeZoneItem.DisplayName + "\" }"),
                       new Microsoft.Azure.Devices.Shared.TwinCollection("{}")
                       );

                    Console.WriteLine("\nAdding new individualEnrollment...");
                    var individualEnrollmentResult =
                        await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                    Console.WriteLine("\nEnrollmentGroup created with success.");
                    Console.WriteLine(individualEnrollmentResult);
                }
                catch (Exception e)
                {

                }


            }
        }
    }
}
