using CertificateManager;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;

namespace DpsWebManagement.Providers;

public class DpsEnrollmentGroupProvider
{
    private IConfiguration Configuration { get;set;}

    private readonly ILogger<DpsEnrollmentGroupProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;
    private readonly ImportExportCertificate _importExportCertificate;
    ProvisioningServiceClient _provisioningServiceClient;

    public DpsEnrollmentGroupProvider(IConfiguration config, ILoggerFactory loggerFactory,
        ImportExportCertificate importExportCertificate,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsEnrollmentGroupProvider>();
        _dpsDbContext = dpsDbContext;
        _importExportCertificate = importExportCertificate;

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

        var cert = _importExportCertificate.PemImportCertificate(dpsCert!.PemPublicKey);

        Attestation attestation = X509Attestation.CreateFromRootCertificates(cert);
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
            Name = enrollmentGroupId
        };
        _dpsDbContext.DpsEnrollmentGroups.Add(newItem);

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

    public List<Model.DpsEnrollmentGroup> GetDpsGroups()
    {
        return _dpsDbContext.DpsEnrollmentGroups.ToList();
    }
}
