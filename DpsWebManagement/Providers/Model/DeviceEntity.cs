
namespace DpsWebManagement.Providers.Model;

public class DeviceEntity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? PemPrivateKey { get; set; }

    public string? PemPublicKey { get; set; }

    public int DpsEnrollmentGroupId { get; set; }

    public DpsEnrollmentGroupEntity DpsEnrollmentGroup { get; set; } = new();
}

