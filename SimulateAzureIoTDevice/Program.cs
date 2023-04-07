using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using CertificateManager;
using System.Security.Cryptography.X509Certificates;

namespace SimulateAzureIoTDevice;

class Program
{
    static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
    static readonly string _pathToCerts = $"{_directory}\\..\\..\\..\\..\\Certs\\";

    // Define the device
    private static readonly string deviceId = "testdevice01";
    private static readonly string iotHubUrl = "damienbod-iothub.azure-devices.net";
    private static readonly TransportType transportType = TransportType.Amqp;

    private const int TEMPERATURE_THRESHOLD = 30;
    private static readonly int MESSAGE_COUNT = 5;
    private static readonly Random rnd = new();
    private static float temperature;
    private static float humidity;

    static void Main(string[] args)
    {
        try
        {
            var serviceProvider = new ServiceCollection()
                .AddCertificateManager().BuildServiceProvider();
            var iec = serviceProvider.GetService<ImportExportCertificate>();

            // PFX
            var password = "1234";
            var deviceId = "coffee-mc"; // "testdevice01";
            var certTestdevice01 = new X509Certificate2($"{_pathToCerts}{deviceId}.pfx", password);

            var auth = new DeviceAuthenticationWithX509Certificate(deviceId, certTestdevice01);
            var deviceClient = DeviceClient.Create(iotHubUrl, auth, transportType);

            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
            }
            else
            {
                Console.WriteLine("Successfully created DeviceClient!");
                SendEvent(deviceClient).Wait();
            }

            Console.WriteLine("Exiting...\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in sample: {0}", ex.Message);
        }
    }

    static async Task SendEvent(DeviceClient deviceClient)
    {
        string dataBuffer;
        Console.WriteLine("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);

        for (int count = 0; count < MESSAGE_COUNT; count++)
        {
            temperature = rnd.Next(20, 35);
            humidity = rnd.Next(60, 80);
            dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", deviceId, count, temperature, humidity);
            var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
            eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
            Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

            await deviceClient.SendEventAsync(eventMessage);
        }
    }
}
