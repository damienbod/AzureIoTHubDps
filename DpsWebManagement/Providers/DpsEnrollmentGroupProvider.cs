using CertificateManager;
using CertificateManager.Models;
using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DpsWebManagement.Providers;

public class DpsEnrollmentGroupProvider
{
    private IConfiguration Configuration { get;set;}

    private readonly ILogger<DpsEnrollmentGroupProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;
    private readonly ImportExportCertificate _importExportCertificate;
    private readonly CreateCertificatesClientServerAuth _createCertsService;
    ProvisioningServiceClient _provisioningServiceClient;

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

        var rootCertificate = _importExportCertificate
            .PemImportCertificate(dpsCert!.PemPrivateKey, dpsCert.Password);

        // create an intermediate for each group
        var certName = $"{enrollmentGroupId}";
        var dpsIntermediateGroup = _createCertsService.NewIntermediateChainedCertificate(
            new DistinguishedName { CommonName = certName, Country = "CH" },
            new ValidityPeriod { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddYears(50) },
            2, certName, rootCertificate);
        //dpsIntermediateGroup.FriendlyName = $"{certName} certificate";

        var password = GetEncodedRandomString(30);
        // get the public key certificate for the enrollment
        var dpsIntermediateGroupPublicPem = _importExportCertificate
            .PemExportPublicKeyCertificate(dpsIntermediateGroup);
        var dpsIntermediateGroupPrivatePem = _importExportCertificate
            .PemExportPfxFullCertificate(dpsIntermediateGroup, password);

        var dpsIntermediateGroupPublic = _importExportCertificate
            .PemImportCertificate(dpsIntermediateGroupPublicPem);

        Attestation attestation = X509Attestation.CreateFromRootCertificates(dpsIntermediateGroupPublicPem);
        EnrollmentGroup enrollmentGroup = new EnrollmentGroup(enrollmentGroupId, attestation)
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
            Password = password,
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

    public async Task QueryEnrollmentGroupAsync()
    {
        _logger.LogInformation("Creating a query for enrollmentGroups...");
        QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
        using (Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification))
        {
            while (query.HasNext())
            {
                _logger.LogInformation("Querying the next enrollmentGroups...");
                QueryResult queryResult = await query.NextAsync();
                _logger.LogInformation("{queryResult}", queryResult);

                foreach (EnrollmentGroup group in queryResult.Items)
                {
                    await EnumerateRegistrationsInGroup(querySpecification, group);
                }
            }
        }
    }

    public async Task<List<DpsEnrollmentGroup>> GetDpsGroupsAsync()
    {
        return await _dpsDbContext.DpsEnrollmentGroups.ToListAsync();
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

    public async Task<DpsEnrollmentGroup?> GetDpsGroupAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentGroups
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
