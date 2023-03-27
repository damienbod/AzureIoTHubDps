using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace DpsManagement;

public class DpsRegisterDevice
{
    private IConfiguration Configuration { get; set; }
    private readonly ILogger<DpsRegisterDevice> _logger;

    public DpsRegisterDevice(IConfiguration config, ILoggerFactory loggerFactory)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDevice>();
    }

    /// <summary>
    /// transport exception if the Common Name "CN=" value within the device x.509 certificate does not match the Group Enrollment name within DPS.
    /// https://github.com/Azure/azure-iot-sdk-c/blob/main/tools/CACertificates/CACertificateOverview.md
    /// </summary>
    public async Task<DeviceRegistrationResult> RegisterDeviceAsync(
        X509Certificate2 deviceCertificate,
        X509Certificate2 enrollmentCertificate)
    {
        var scopeId = Configuration["ScopeId"];

        using (var security = new SecurityProviderX509Certificate(deviceCertificate, new X509Certificate2Collection(enrollmentCertificate)))

        // To optimize for size, reference only the protocols used by your application.
        using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
        //using (var transport = new ProvisioningTransportHandlerHttp())
        //using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
        // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
        {
            var client = ProvisioningDeviceClient.Create("global.azure-devices-provisioning.net", scopeId, security, transport);

            var result = await client.RegisterAsync();
            _logger.LogInformation("DPS client created: {result}", result);
            return result;
        }
    }
}
