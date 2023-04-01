using DpsWebManagement.Providers.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace DpsWebManagement.Providers;

public class DpsDbContext : DbContext
{
    public DpsDbContext(DbContextOptions<DpsDbContext> options): base(options)
    {
    }

    public DbSet<DpsEnrollmentDevice> DpsEnrollmentDevices => Set<DpsEnrollmentDevice>();
    public DbSet<Model.DpsEnrollmentGroup> DpsEnrollmentGroups => base.Set<Model.DpsEnrollmentGroup>();
    public DbSet<DpsCertificate> DpsCertificates => Set<DpsCertificate>();
  
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DpsEnrollmentDevice>().HasKey(m => m.Id);
        builder.Entity<Model.DpsEnrollmentGroup>().HasKey(m => m.Id);
        builder.Entity<DpsCertificate>().HasKey(m => m.Id);

        builder.Entity<Model.DpsCertificate>()
          .HasMany(e => e.DpsEnrollmentGroups)
          .WithOne(e => e.DpsCertificate)
          .HasForeignKey(e => e.DpsCertificateId)
          .HasPrincipalKey(e => e.Id);

        builder.Entity<Model.DpsEnrollmentGroup>()
           .HasMany(e => e.DpsEnrollmentDevices)
           .WithOne(e => e.DpsEnrollmentGroup)
           .HasForeignKey(e => e.DpsEnrollmentGroupId)
           .HasPrincipalKey(e => e.Id);

        base.OnModelCreating(builder);
    }
}