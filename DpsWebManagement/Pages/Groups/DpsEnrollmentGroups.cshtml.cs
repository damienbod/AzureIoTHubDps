using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DpsWebManagement.Pages.Groups;

public class DpsEnrollmentGroupsModel : PageModel
{
    private readonly DpsEnrollmentGroupProvider _dpsEnrollmentGroup;

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public List<DpsEnrollmentGroupModel> DpsEnrollmentGroups { get; set; } = new List<DpsEnrollmentGroupModel>();

    public DpsEnrollmentGroupsModel(DpsEnrollmentGroupProvider dpsEnrollmentGroup)
    {
        _dpsEnrollmentGroup = dpsEnrollmentGroup;
    }

    public async Task OnGetAsync()
    {
        var data = await _dpsEnrollmentGroup.GetDpsGroupsAsync(Id);
        DpsEnrollmentGroups = data.Select(item => new DpsEnrollmentGroupModel
        {
            Id = item.Id,
            Name = item.Name,
            DpsCertificate = item.DpsCertificateId
        }).ToList();
    }
}

public class DpsEnrollmentGroupModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? DpsCertificate { get; set; }
}