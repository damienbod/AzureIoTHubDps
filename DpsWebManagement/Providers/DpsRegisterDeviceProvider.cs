﻿using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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

    static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
    static readonly string _pathToCerts = $"{_directory}\\..\\..\\..\\..\\Certs\\";

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
          new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
          $"{commonNameDeviceId}", dpsEnrollmentGroupCertificate);
        //deviceCertificate.FriendlyName = $"IoT device {commonNameDeviceId}";

        var deviceInPfxBytes = _importExportCertificate
            .ExportChainedCertificatePfx(password, deviceCertificate, dpsEnrollmentGroupCertificate);

        File.WriteAllBytes($"{_pathToCerts}{commonNameDeviceId}.pfx", deviceInPfxBytes);

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

        var newItem = new Model.DpsEnrollmentDevice
        {
            Password = password,
            PemPublicKey = $"{_pathToCerts}{commonNameDeviceId}.pfx",
            PemPrivateKey = deviceCertPrivatePem,
            Name = commonNameDeviceId,
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
