using DpsWebManagement.Providers.Model;
using Microsoft.EntityFrameworkCore;

namespace DpsWebManagement.Providers;

public class DpsDbContext : DbContext
{
    public DpsDbContext(DbContextOptions<DpsDbContext> options): base(options)
    {
    }

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<DpsEnrollmentGroupEntity> DpsEnrollmentGroups => Set<DpsEnrollmentGroupEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DeviceEntity>().HasKey(m => m.Id);
        builder.Entity<DpsEnrollmentGroupEntity>().HasKey(m => m.Id);

        base.OnModelCreating(builder);
    }
}