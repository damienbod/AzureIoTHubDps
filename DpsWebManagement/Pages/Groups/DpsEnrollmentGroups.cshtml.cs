using DpsWebManagement.Providers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DpsWebManagement.Pages.Groups;

public class DpsEnrollmentGroupsModel : PageModel
{
    private readonly DpsEnrollmentGroupProvider _dpsEnrollmentGroup;

    public List<DpsEnrollmentGroupModel> DpsEnrollmentGroups { get; set; } = new List<DpsEnrollmentGroupModel>();

    public DpsEnrollmentGroupsModel(DpsEnrollmentGroupProvider dpsEnrollmentGroup)
    {
        _dpsEnrollmentGroup = dpsEnrollmentGroup;
    }

    public async Task OnGetAsync()
    {
        var data = await _dpsEnrollmentGroup.GetDpsGroupsAsync();
        DpsEnrollmentGroups =  data.Select(item => new DpsEnrollmentGroupModel
        {
            Id = item.Id,
            Name = item.Name
        }).ToList();
    }
}

public class DpsEnrollmentGroupModel
{
    public int Id { get; set; }

    public string? Name { get; set; }
}