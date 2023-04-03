using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace DpsManagement;

public class DpsEnrollmentGroup
{
    private IConfiguration Configuration { get;set;}

    private readonly ILogger<DpsEnrollmentGroup> _logger;
    ProvisioningServiceClient _provisioningServiceClient;
    public DpsEnrollmentGroup(IConfiguration config, ILoggerFactory loggerFactory)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsEnrollmentGroup>();

        _provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(
                  Configuration.GetConnectionString("DpsConnection"));
    }

    public async Task CreateDpsEnrollmentGroupAsync(string enrollmentGroupId, X509Certificate2 pemCertificate)
    {
        _logger.LogInformation("Starting CreateDpsEnrollmentGroupAsync...");
        _logger.LogInformation("Creating a new enrollmentGroup...");
       
        Attestation attestation = X509Attestation.CreateFromRootCertificates(pemCertificate);
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
}
