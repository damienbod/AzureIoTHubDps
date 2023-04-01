
namespace DpsWebManagement.Providers.Model;

public class DpsEnrollmentDevice
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? PemPrivateKey { get; set; }

    public string? PemPublicKey { get; set; }

    public int DpsEnrollmentGroupId { get; set; }

    public DpsEnrollmentGroup DpsEnrollmentGroup { get; set; } = new();
}

