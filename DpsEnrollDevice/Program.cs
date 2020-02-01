using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DpsEnrollDevice
{
    public static class Program
    {
        // The Provisioning Hub IDScope.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the DPS_IDSCOPE environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");

        // In your Device Provisioning Service please go to "Manage enrollments" and select "Individual Enrollments".
        // Select "Add individual enrollment" then fill in the following:
        // Mechanism: X.509
        // Certificate: 
        //    You can generate a self-signed certificate by running the GenerateTestCertificate.ps1 powershell script.
        //    Select the public key 'certificate.cer' file. ('certificate.pfx' contains the private key and is password protected.)
        //    For production code, it is advised that you install the certificate in the CurrentUser (My) store.
        // DeviceID: iothubx509device1

        // X.509 certificates may also be used for enrollment groups.
        // In your Device Provisioning Service please go to "Manage enrollments" and select "Enrollment Groups".
        // Select "Add enrollment group" then fill in the following:
        // Group name: <your  group name>
        // Attestation Type: Certificate
        // Certificate Type: 
        //    choose CA certificate then link primary and secondary certificates 
        //    OR choose Intermediate certificate and upload primary and secondary certificate files
        // You may also change other enrollemtn group parameters according to your needs

        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";

        public static int Main(string[] args)
        {

            var scopeId = "0ne000BC0AC";
            X509Certificate2 certificate = new X509Certificate2("testdevice01.pfx", "1234");
            // The cert from the enrollment group is required for group registrations
            //X509Certificate2 enrollCert = new X509Certificate2("dpsIntermediate1.pfx", "1234");

            using (var security = new SecurityProviderX509Certificate(certificate))
            // new X509Certificate2Collection(enrollCert)))

            // Select one of the available transports:
            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                var client = ProvisioningDeviceClient.Create(
                    "global.azure-devices-provisioning.net", scopeId, security, transport);

                var result = client.RegisterAsync().GetAwaiter().GetResult();
            }

            return 0;
        }

    
    }
}