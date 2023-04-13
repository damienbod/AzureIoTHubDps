using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
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
    private readonly ImportExportCertificate _iec;
    private readonly CreateCertificatesClientServerAuth _createCertsService;

    public DpsRegisterDeviceProvider(IConfiguration config, 
        ILoggerFactory loggerFactory,
        ImportExportCertificate importExportCertificate,
        CreateCertificatesClientServerAuth ccs,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDeviceProvider>();
        _dpsDbContext = dpsDbContext;
        _iec = importExportCertificate;
        _createCertsService = ccs;
    }

    public async Task<(int? DeviceId, string? ErrorMessage)> RegisterDeviceAsync(
        string deviceCommonNameDevice, string dpsEnrollmentGroupId)
    {
        var scopeId = Configuration["ScopeId"];

        var dpsEnrollmentGroup = _dpsDbContext.DpsEnrollmentGroups
           .FirstOrDefault(t => t.Id == int.Parse(dpsEnrollmentGroupId));

        var certDpsEnrollmentGroup = X509Certificate2.CreateFromPem(
            dpsEnrollmentGroup!.PemPublicKey, dpsEnrollmentGroup.PemPrivateKey);

        var newDevice = new DpsEnrollmentDevice
        {
            Password = GetEncodedRandomString(30),
            Name = deviceCommonNameDevice.ToLower(),
            DpsEnrollmentGroupId = dpsEnrollmentGroup.Id,
            DpsEnrollmentGroup = dpsEnrollmentGroup
        };

        var certDevice = _createCertsService.NewDeviceChainedCertificate(
          new DistinguishedName { CommonName = $"{newDevice.Name}" },
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
          $"{newDevice.Name}", certDpsEnrollmentGroup);

        var deviceInPfxBytes = _iec.ExportChainedCertificatePfx(newDevice.Password, 
            certDevice, certDpsEnrollmentGroup);

        // This is required if you want PFX exports to work.
        newDevice.PathToPfx = FileProvider.WritePfxToDisk($"{newDevice.Name}.pfx", deviceInPfxBytes);

        // get the public key certificate for the device
        newDevice.PemPublicKey = _iec.PemExportPublicKeyCertificate(certDevice);
        FileProvider.WriteToDisk($"{newDevice.Name}-public.pem", newDevice.PemPublicKey);

        using (ECDsa? ecdsa = certDevice.GetECDsaPrivateKey())
        {
            newDevice.PemPrivateKey = ecdsa!.ExportECPrivateKeyPem();
            FileProvider.WriteToDisk($"{newDevice.Name}-private.pem", newDevice.PemPrivateKey);
        }

        // setup Windows store deviceCert 
        var pemExportDevice = _iec.PemExportPfxFullCertificate(certDevice, newDevice.Password);
        var certDeviceForCreation = _iec.PemImportCertificate(pemExportDevice, newDevice.Password);

        using (var security = new SecurityProviderX509Certificate(certDeviceForCreation, new X509Certificate2Collection(certDpsEnrollmentGroup)))

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

                newDevice.AssignedHub = result.AssignedHub;
                newDevice.DeviceId = result.DeviceId;
                newDevice.RegistrationId = result.RegistrationId;

                _logger.LogInformation("DPS client created: {result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError("DPS client created: {result}", ex.Message);
                return (null, ex.Message);
            }
        }

        _dpsDbContext.DpsEnrollmentDevices.Add(newDevice);
        dpsEnrollmentGroup.DpsEnrollmentDevices.Add(newDevice);
        await _dpsDbContext.SaveChangesAsync();

        return (newDevice.Id, null);
    }

    private static string GetEncodedRandomString(int length)
    {
        var base64 = Convert.ToBase64String(GenerateRandomBytes(length));
        return base64;
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var byteArray = new byte[length];
        RandomNumberGenerator.Fill(byteArray);
        return byteArray;
    }

    public async Task<List<DpsEnrollmentDevice>> GetDpsDevicesAsync(int? dpsEnrollmentGroupId)
    {
        if(dpsEnrollmentGroupId == null)
        {
            return await _dpsDbContext.DpsEnrollmentDevices.ToListAsync();
        }

        return await _dpsDbContext.DpsEnrollmentDevices.Where(s => s.DpsEnrollmentGroupId == dpsEnrollmentGroupId).ToListAsync();
    }

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .Include(device => device.DpsEnrollmentGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
