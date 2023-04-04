using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DpsWebManagement.Providers;

public class DpsRegisterDeviceProvider
{
    private IConfiguration Configuration { get; set; }
    private readonly ILogger<DpsRegisterDeviceProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;
    private readonly ImportExportCertificate _importExportCertificate;
    private readonly CreateCertificatesClientServerAuth _createCertsService;

    public DpsRegisterDeviceProvider(IConfiguration config, 
        ILoggerFactory loggerFactory,
        ImportExportCertificate importExportCertificate,
        CreateCertificatesClientServerAuth createCertificatesClientServerAuth,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDeviceProvider>();
        _dpsDbContext = dpsDbContext;
        _importExportCertificate = importExportCertificate;
        _createCertsService = createCertificatesClientServerAuth;
    }

    /// <summary>
    /// transport exception if the Common Name "CN=" value within the device x.509 certificate does not match the Group Enrollment name within DPS.
    /// https://github.com/Azure/azure-iot-sdk-c/blob/main/tools/CACertificates/CACertificateOverview.md
    /// </summary>
    public async Task<int?> RegisterDeviceAsync(
        string commonNameDeviceId, string dpsEnrollmentGroupId)
    {
        int? deviceId = null;
        var scopeId = Configuration["ScopeId"];
        string? password = GetEncodedRandomString(30);
        commonNameDeviceId = commonNameDeviceId.ToLower();

        var dpsEnrollmentGroup = _dpsDbContext.DpsEnrollmentGroups
           .FirstOrDefault(t => t.Id == int.Parse(dpsEnrollmentGroupId));

        var dpsEnrollmentGroupCertificate = _importExportCertificate
            .PemImportCertificate(dpsEnrollmentGroup!.PemPrivateKey, dpsEnrollmentGroup.Password);

        var deviceCertificate = _createCertsService.NewDeviceChainedCertificate(
          new DistinguishedName { CommonName = $"{commonNameDeviceId}" },
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(10) },
          $"{commonNameDeviceId}", dpsEnrollmentGroupCertificate);
        deviceCertificate.FriendlyName = $"IoT device {commonNameDeviceId}";

        var deviceInPfxBytes = _importExportCertificate
            .ExportChainedCertificatePfx("1234", deviceCertificate, dpsEnrollmentGroupCertificate);
     
        // Add this for manual testing
        // File.WriteAllBytes($"{commonNameDeviceId}.pfx", deviceInPfxBytes);

        // get the public key certificate for the enrollment
        var deviceCertPublicPem = _importExportCertificate
            .PemExportPublicKeyCertificate(deviceCertificate);
        var deviceCertPrivatePem = _importExportCertificate
            .PemExportPfxFullCertificate(deviceCertificate, password);

        var deviceCert = _importExportCertificate
            .PemImportCertificate(deviceCertPrivatePem, password);

        using (var security = new SecurityProviderX509Certificate(deviceCert, new X509Certificate2Collection(dpsEnrollmentGroupCertificate)))

        // To optimize for size, reference only the protocols used by your application.
        using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
        //using (var transport = new ProvisioningTransportHandlerHttp())
        //using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
        // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
        {
            var client = ProvisioningDeviceClient
                .Create("global.azure-devices-provisioning.net", scopeId, security, transport);

            var result = await client.RegisterAsync();
            _logger.LogInformation("DPS client created: {result}", result);
            //return result;
        }

        var newItem = new Model.DpsEnrollmentDevice
        {
            Password = password,
            PemPublicKey = deviceCertPublicPem,
            PemPrivateKey = deviceCertPrivatePem,
            Name = commonNameDeviceId,
            DpsEnrollmentGroupId = dpsEnrollmentGroup.Id,
            DpsEnrollmentGroup = dpsEnrollmentGroup
        };
        _dpsDbContext.DpsEnrollmentDevices.Add(newItem);

        dpsEnrollmentGroup.DpsEnrollmentDevices.Add(newItem);

        await _dpsDbContext.SaveChangesAsync();

        deviceId = newItem.Id;
        return deviceId;
    }

    private string GetEncodedRandomString(int length)
    {
        var base64 = Convert.ToBase64String(GenerateRandomBytes(length));
        return base64;
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var byteArray = new byte[length];
        RandomNumberGenerator.Fill(byteArray);
        return byteArray;
    }

    public async Task<List<DpsEnrollmentDevice>> GetDpsDevicesAsync()
    {
        return await _dpsDbContext.DpsEnrollmentDevices.ToListAsync();
    }

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
