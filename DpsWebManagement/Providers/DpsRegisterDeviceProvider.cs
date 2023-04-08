using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
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
    public async Task<(int? DeviceId, string? ErrorMessage)> RegisterDeviceAsync(
        string deviceCommonNameDevice, string dpsEnrollmentGroupId)
    {
        int? deviceId = null;
        var scopeId = Configuration["ScopeId"];
        string? password = GetEncodedRandomString(30);
        deviceCommonNameDevice = deviceCommonNameDevice.ToLower();

        var dpsEnrollmentGroup = _dpsDbContext.DpsEnrollmentGroups
           .FirstOrDefault(t => t.Id == int.Parse(dpsEnrollmentGroupId));

        var dpsEnrollmentGroupCert = X509Certificate2.CreateFromPem(
            dpsEnrollmentGroup!.PemPublicKey, dpsEnrollmentGroup.PemPrivateKey);

        var deviceCertificate = _createCertsService.NewDeviceChainedCertificate(
          new DistinguishedName { CommonName = $"{deviceCommonNameDevice}" },
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
          $"{deviceCommonNameDevice}", dpsEnrollmentGroupCert);
        //deviceCertificate.FriendlyName = $"IoT device {deviceCommonNameDevice}";

        var deviceInPfxBytes = _importExportCertificate
            .ExportChainedCertificatePfx(password, deviceCertificate, dpsEnrollmentGroupCert);

        // This is required if you want PFX exports to work.
        var pfxPath = FileProvider.WritePfxToDisk($"{deviceCommonNameDevice}.pfx", deviceInPfxBytes);

        // get the public key certificate for the device
        var deviceCertPublicPem = _importExportCertificate
            .PemExportPublicKeyCertificate(deviceCertificate);
        FileProvider.WriteToDisk($"{deviceCommonNameDevice}-public.pem", deviceCertPublicPem);

        string deviceCertPrivateKeyPem = string.Empty;
        using (ECDsa? ecdsa = deviceCertificate.GetECDsaPrivateKey())
        {
            deviceCertPrivateKeyPem = ecdsa!.ExportECPrivateKeyPem();
            FileProvider.WriteToDisk($"{deviceCommonNameDevice}-private.pem", deviceCertPrivateKeyPem);
        }

        // setup Windows store deviceCert 
        var exportDevice = _importExportCertificate
            .PemExportPfxFullCertificate(deviceCertificate, password);
        var deviceCert = _importExportCertificate
            .PemImportCertificate(exportDevice, password);

        using (var security = new SecurityProviderX509Certificate(deviceCert, new X509Certificate2Collection(dpsEnrollmentGroupCert)))

        // To optimize for size, reference only the protocols used by your application.
        using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
        //using (var transport = new ProvisioningTransportHandlerHttp())
        //using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
        //using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
        {
            var client = ProvisioningDeviceClient.Create("global.azure-devices-provisioning.net",
                scopeId, security, transport);

            try
            {
                var result = await client.RegisterAsync();
                _logger.LogInformation("DPS client created: {result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError("DPS client created: {result}", ex.Message);
                return (null, ex.Message);
            }
        }

        var newItem = new DpsEnrollmentDevice
        {
            Password = password,
            PathToPfx = pfxPath,
            PemPrivateKey = deviceCertPrivateKeyPem,
            PemPublicKey = deviceCertPublicPem,
            Name = deviceCommonNameDevice,
            DpsEnrollmentGroupId = dpsEnrollmentGroup.Id,
            DpsEnrollmentGroup = dpsEnrollmentGroup
        };
        _dpsDbContext.DpsEnrollmentDevices.Add(newItem);

        dpsEnrollmentGroup.DpsEnrollmentDevices.Add(newItem);

        await _dpsDbContext.SaveChangesAsync();

        deviceId = newItem.Id;
        return (deviceId, null);
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

    public async Task<List<DpsEnrollmentDevice>> GetDpsDevicesAsync(int? dpsEnrollmentGroupId)
    {
        if(dpsEnrollmentGroupId == null)
            return await _dpsDbContext.DpsEnrollmentDevices.ToListAsync();

        return await _dpsDbContext.DpsEnrollmentDevices.Where(s => s.DpsEnrollmentGroupId == dpsEnrollmentGroupId).ToListAsync();
    }

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .Include(device => device.DpsEnrollmentGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
