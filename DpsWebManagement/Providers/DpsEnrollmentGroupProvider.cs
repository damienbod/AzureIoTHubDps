using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DpsWebManagement.Providers;

public class DpsEnrollmentGroupProvider
{
    private IConfiguration Configuration { get;set;}

    private readonly ILogger<DpsEnrollmentGroupProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;
    private readonly ImportExportCertificate _importExportCertificate;
    private readonly CreateCertificatesClientServerAuth _createCertsService;
    private readonly ProvisioningServiceClient _provisioningServiceClient;
    
    static readonly string? _directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);   
    static readonly string _pathToCerts = $"{_directory}\\..\\..\\..\\..\\Certs\\";

    public DpsEnrollmentGroupProvider(IConfiguration config, ILoggerFactory loggerFactory,
        ImportExportCertificate importExportCertificate,
        CreateCertificatesClientServerAuth createCertificatesClientServerAuth,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsEnrollmentGroupProvider>();
        _dpsDbContext = dpsDbContext;
        _importExportCertificate = importExportCertificate;
        _createCertsService = createCertificatesClientServerAuth;

        _provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(
                  Configuration.GetConnectionString("DpsConnection"));
    }

    public async Task<(string Name, int Id)> CreateDpsEnrollmentGroupAsync(
        string enrollmentGroupId,
        string certificatePublicPemId)
    {
        _logger.LogInformation("Starting CreateDpsEnrollmentGroupAsync...");
        _logger.LogInformation("Creating a new enrollmentGroup...");

        var dpsCert = _dpsDbContext.DpsCertificates
            .FirstOrDefault(t => t.Id == int.Parse(certificatePublicPemId));

        var rootCertificate = X509Certificate2.CreateFromPem(
               dpsCert!.PemPublicKey,
               dpsCert.PemPrivateKey);

        // create an intermediate for each group
        var certName = $"{enrollmentGroupId}";
        var dpsIntermediateGroup = _createCertsService.NewIntermediateChainedCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            2, certName, rootCertificate);
        //dpsIntermediateGroup.FriendlyName = $"{certName} certificate";

        // get the public key certificate for the enrollment
        var dpsIntermediateGroupPublicPem = _importExportCertificate
            .PemExportPublicKeyCertificate(dpsIntermediateGroup);

        string dpsIntermediateGroupPrivatePem = string.Empty;
        using (ECDsa? ecdsa = dpsIntermediateGroup.GetECDsaPrivateKey())
        {
            dpsIntermediateGroupPrivatePem = ecdsa!.ExportECPrivateKeyPem();
            File.WriteAllText($"{_pathToCerts}{enrollmentGroupId}-private.pem", 
                dpsIntermediateGroupPrivatePem);
        }

        var dpsIntermediateGroupPublic = _importExportCertificate
            .PemImportCertificate(dpsIntermediateGroupPublicPem);

        Attestation attestation = X509Attestation.CreateFromRootCertificates(dpsIntermediateGroupPublicPem);
        var enrollmentGroup = new EnrollmentGroup(enrollmentGroupId, attestation)
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
        _logger.LogInformation("{enrollmentGroup}", enrollmentGroup);
        _logger.LogInformation("Adding new enrollmentGroup...");

        EnrollmentGroup enrollmentGroupResult = await _provisioningServiceClient
            .CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup);

        _logger.LogInformation("EnrollmentGroup created with success.");
        _logger.LogInformation("{enrollmentGroupResult}", enrollmentGroupResult);

        var newItem = new Model.DpsEnrollmentGroup
        {
            DpsCertificateId = dpsCert.Id,
            Name = enrollmentGroupId, 
            DpsCertificate = dpsCert,
            PemPublicKey = dpsIntermediateGroupPublicPem,
            PemPrivateKey = dpsIntermediateGroupPrivatePem
        };
        _dpsDbContext.DpsEnrollmentGroups.Add(newItem);

        dpsCert.DpsEnrollmentGroups.Add(newItem);

        await _dpsDbContext.SaveChangesAsync();

        return (newItem.Name, newItem.Id);
    }

    public async Task EnumerateRegistrationsInGroup(QuerySpecification querySpecification, EnrollmentGroup group)
    {
        _logger.LogInformation("Creating a query for registrations within group '{groupEnrollmentGroupId}'...", group.EnrollmentGroupId);
        using (Query registrationQuery = _provisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(querySpecification, group.EnrollmentGroupId))
        {
            _logger.LogInformation("Querying the next registrations within group '{groupEnrollmentGroupId}'...", group.EnrollmentGroupId);
            QueryResult registrationQueryResult = await registrationQuery.NextAsync();
            _logger.LogInformation("{registrationQueryResult}", registrationQueryResult);
        }
    }

    public async Task<List<DpsEnrollmentGroup>> GetDpsGroupsAsync(int? certificateId = null)
    {
        if (certificateId == null)
            return await _dpsDbContext.DpsEnrollmentGroups.ToListAsync();

        return await _dpsDbContext.DpsEnrollmentGroups.Where(s => s.DpsCertificateId == certificateId).ToListAsync();
    }

    public async Task<DpsEnrollmentGroup?> GetDpsGroupAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentGroups
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
