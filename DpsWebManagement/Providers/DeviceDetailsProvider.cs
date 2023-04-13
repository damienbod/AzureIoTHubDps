using DpsWebManagement.Providers.Model;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.EntityFrameworkCore;

namespace DpsWebManagement.Providers;

public class DeviceDetailsProvider
{
    private IConfiguration Configuration { get; set; }
    private readonly ILogger<DpsRegisterDeviceProvider> _logger;
    private readonly DpsDbContext _dpsDbContext;

    private readonly ProvisioningServiceClient _provisioningServiceClient;

    public DeviceDetailsProvider(IConfiguration config, 
        ILoggerFactory loggerFactory,
        DpsDbContext dpsDbContext)
    {
        Configuration = config;
        _logger = loggerFactory.CreateLogger<DpsRegisterDeviceProvider>();
        _dpsDbContext = dpsDbContext;

        _provisioningServiceClient = ProvisioningServiceClient
              .CreateFromConnectionString(Configuration.GetConnectionString("DpsConnection"));
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

    public async Task<DpsEnrollmentDevice?> GetDpsDeviceAsync(int id)
    {
        return await _dpsDbContext.DpsEnrollmentDevices
            .Include(device => device.DpsEnrollmentGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
