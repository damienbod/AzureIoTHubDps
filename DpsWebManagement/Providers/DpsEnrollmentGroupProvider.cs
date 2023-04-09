using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DpsWebManagement.Providers;

public class DpsEnrollmentGroupProvider
{
    private IConfiguration Configuration { get;set;}

    private readonly ILogger<DpsEnrollmentGroupProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;
    private readonly ImportExportCertificate _iec;
    private readonly CreateCertificatesClientServerAuth _createCertsService;
    private readonly ProvisioningServiceClient _provisioningServiceClient;
    
    public DpsEnrollmentGroupProvider(IConfiguration config, ILoggerFactory loggerFactory,
        ImportExportCertificate importExportCertificate,
        CreateCertificatesClientServerAuth ccs,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsEnrollmentGroupProvider>();
        _dpsDbContext = dpsDbContext;
        _iec = importExportCertificate;
        _createCertsService = ccs;

        _provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(
                  Configuration.GetConnectionString("DpsConnection"));
    }

    public async Task<(string Name, int Id)> CreateDpsEnrollmentGroupAsync(
        string enrollmentGroupName, string certificatePublicPemId)
    {
        _logger.LogInformation("Starting CreateDpsEnrollmentGroupAsync...");
        _logger.LogInformation("Creating a new enrollmentGroup...");

        var dpsCertificate = _dpsDbContext.DpsCertificates
            .FirstOrDefault(t => t.Id == int.Parse(certificatePublicPemId));

        var rootCertificate = X509Certificate2.CreateFromPem(
               dpsCertificate!.PemPublicKey, dpsCertificate.PemPrivateKey);

        // create an intermediate for each group
        var certName = $"{enrollmentGroupName}";
        var certDpsGroup = _createCertsService.NewIntermediateChainedCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            2, certName, rootCertificate);

        // get the public key certificate for the enrollment
        var pemDpsGroupPublic = _iec.PemExportPublicKeyCertificate(certDpsGroup);

        string pemDpsGroupPrivate = string.Empty;
        using (ECDsa? ecdsa = certDpsGroup.GetECDsaPrivateKey())
        {
            pemDpsGroupPrivate = ecdsa!.ExportECPrivateKeyPem();
            FileProvider.WriteToDisk($"{enrollmentGroupName}-private.pem", pemDpsGroupPrivate);
        }

        Attestation attestation = X509Attestation.CreateFromRootCertificates(pemDpsGroupPublic);
        EnrollmentGroup enrollmentGroup = CreateEnrollmentGroup(enrollmentGroupName, attestation);

        _logger.LogInformation("{enrollmentGroup}", enrollmentGroup);
        _logger.LogInformation("Adding new enrollmentGroup...");

        EnrollmentGroup enrollmentGroupResult = await _provisioningServiceClient
            .CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup);

        _logger.LogInformation("EnrollmentGroup created with success.");
        _logger.LogInformation("{enrollmentGroupResult}", enrollmentGroupResult);

        DpsEnrollmentGroup newItem = await PersistData(enrollmentGroupName, 
            dpsCertificate, pemDpsGroupPublic, pemDpsGroupPrivate);

        return (newItem.Name, newItem.Id);
    }

    private async Task<DpsEnrollmentGroup> PersistData(string enrollmentGroupName, 
        DpsCertificate dpsCertificate, string pemDpsGroupPublic, string pemDpsGroupPrivate)
    {
        var newItem = new DpsEnrollmentGroup
        {
            DpsCertificateId = dpsCertificate.Id,
            Name = enrollmentGroupName,
            DpsCertificate = dpsCertificate,
            PemPublicKey = pemDpsGroupPublic,
            PemPrivateKey = pemDpsGroupPrivate
        };

        _dpsDbContext.DpsEnrollmentGroups.Add(newItem);
        dpsCertificate.DpsEnrollmentGroups.Add(newItem);

        await _dpsDbContext.SaveChangesAsync();
        return newItem;
    }

    private static EnrollmentGroup CreateEnrollmentGroup(string enrollmentGroupName, Attestation attestation)
    {
        return new EnrollmentGroup(enrollmentGroupName, attestation)
        {
            ProvisioningStatus = ProvisioningStatus.Enabled,
            ReprovisionPolicy = new ReprovisionPolicy
            {
                MigrateDeviceData = false,
                UpdateHubAssignment = true
            },
            Capabilities = new DeviceCapabilities
            {
                IotEdge = false
            },
            InitialTwinState = new TwinState(
                new TwinCollection("{ \"updatedby\":\"" + "damien" + "\", \"timeZone\":\"" + TimeZoneInfo.Local.DisplayName + "\" }"),
                new TwinCollection("{ }")
            )
        };
    }

    public async Task<List<DpsEnrollmentGroup>> GetDpsGroupsAsync(int? certificateId = null)
    {
        if (certificateId == null)
        {
            return await _dpsDbContext.DpsEnrollmentGroups.ToListAsync();
        }

        return await _dpsDbContext.DpsEnrollmentGroups
            .Where(s => s.DpsCertificateId == certificateId).ToListAsync();
    }

    public async Task<DpsEnrollmentGroup?> GetDpsGroupAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentGroups
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
