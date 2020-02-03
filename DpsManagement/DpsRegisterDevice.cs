using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace DpsManagement
{
    public class DpsRegisterDevice
    {
        public async Task<DeviceRegistrationResult> RegisterDeviceAsync()
        {
            var scopeId = "0ne000BC0AC";
            X509Certificate2 certificate = new X509Certificate2("testdevice02.pfx", "1234");
            // The cert from the enrollment group is required for group registrations
            X509Certificate2 enrollCert = new X509Certificate2("dpsIntermediate1.pfx", "1234");

            using (var security = new SecurityProviderX509Certificate(certificate,
                new X509Certificate2Collection(enrollCert)))

            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                var client = ProvisioningDeviceClient.Create(
                    "global.azure-devices-provisioning.net", scopeId, security, transport);

                var result = await client.RegisterAsync();
                return result;
            }
        }
    }
}
