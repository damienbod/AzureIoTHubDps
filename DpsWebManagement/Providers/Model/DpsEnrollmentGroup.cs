﻿namespace DpsWebManagement.Providers.Model;

public class DpsEnrollmentGroup
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? PemPrivateKey { get; set; }

    public string? PemPublicKey { get; set; }

    public int DpsCertificateId { get; set; }

    public DpsCertificate DpsCertificate { get; set; } = new();

    public ICollection<DpsEnrollmentDevice> DpsEnrollmentDevices { get; } = new List<DpsEnrollmentDevice>();  
}