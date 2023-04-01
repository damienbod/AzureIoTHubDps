namespace DpsWebManagement.Providers.Model;

public class DpsEnrollmentGroupEntity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? PemPrivateKey { get; set; }

    public string? PemPublicKey { get; set; }
}