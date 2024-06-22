using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DpsManagement;

public class DpsUpdateDevice
{
    private readonly ProvisioningServiceClient _provisioningServiceClient;

    private IConfiguration Configuration { get; set; }
    private readonly ILogger<DpsUpdateDevice> _logger;

    public DpsUpdateDevice(IConfiguration config, ILoggerFactory loggerFactory)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsUpdateDevice>();

        _provisioningServiceClient = ProvisioningServiceClient
             .CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection"));
    }

    public async Task DisableEnrollmentGroupAsync(string enrollmentGroupId)
    {
        var groupEnrollment = await _provisioningServiceClient.GetEnrollmentGroupAsync(enrollmentGroupId);

        if (groupEnrollment != null && groupEnrollment.ProvisioningStatus != null
            && groupEnrollment.ProvisioningStatus.Value != ProvisioningStatus.Disabled)
        {
            groupEnrollment.ProvisioningStatus = ProvisioningStatus.Disabled;
            var update = await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(groupEnrollment);
            _logger.LogInformation("DisableEnrollmentGroupAsync update.ProvisioningStatus: ", update.ProvisioningStatus);
        }
    }

    public async Task EnableEnrollmentGroupAsync(string enrollmentGroupId)
    {
        QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments"); // WHERE does not work here..., can only do a select ALL
        var groupEnrollments = await _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification).NextAsync();

        foreach (var devicestring in groupEnrollments.Items)
        {
            var enrollment = devicestring as EnrollmentGroup;
            if (enrollment != null && enrollment.EnrollmentGroupId == enrollmentGroupId)
            {
                if (enrollment.ProvisioningStatus != null &&
                    enrollment.ProvisioningStatus.Value != ProvisioningStatus.Enabled)
                {
                    enrollment.ProvisioningStatus = ProvisioningStatus.Enabled;
                    var update = await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollment);
                    _logger.LogInformation("EnableEnrollmentGroupAsync update.ProvisioningStatus: ", update.ProvisioningStatus);
                }
            }
        }
    }
}
