using Microsoft.EntityFrameworkCore;

namespace DpsWebManagement.Providers;

public class DpsDbContext : DbContext
{
    public DpsDbContext(DbContextOptions<DpsDbContext> options): base(options)
    {
    }
}