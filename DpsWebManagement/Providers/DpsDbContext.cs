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

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<DpsEnrollmentGroupEntity> DpsEnrollmentGroups => Set<DpsEnrollmentGroupEntity>();
    public DbSet<DpsCertificateEntity> DpsCertificates => Set<DpsCertificateEntity>();
  
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DeviceEntity>().HasKey(m => m.Id);
        builder.Entity<DpsEnrollmentGroupEntity>().HasKey(m => m.Id);
        builder.Entity<DpsCertificateEntity>().HasKey(m => m.Id);

        builder.Entity<DpsEnrollmentGroupEntity>()
           .HasMany(e => e.Devices)
           .WithOne(e => e.DpsEnrollmentGroup)
           .HasForeignKey(e => e.DpsEnrollmentGroupId)
           .HasPrincipalKey(e => e.Id);

        base.OnModelCreating(builder);
    }
}