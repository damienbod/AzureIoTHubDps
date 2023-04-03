
namespace DpsWebManagement.Providers.Model;

public class DpsEnrollmentDevice
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? PemPrivateKey { get; set; }

    public string? PemPublicKey { get; set; }

    // This should be encrypted or not saved all all here! 
    public string? Password { get; set; }

    public int DpsEnrollmentGroupId { get; set; }

    public DpsEnrollmentGroup DpsEnrollmentGroup { get; set; } = new();
}

