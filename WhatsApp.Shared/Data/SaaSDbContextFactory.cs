using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WhatsApp.Shared.Data
{
    /// <summary>
    /// Design-time factory for creating SaaSDbContext for EF Core migrations
    /// </summary>
    public class SaaSDbContextFactory : IDesignTimeDbContextFactory<SaaSDbContext>
    {
        public SaaSDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SaaSDbContext>();

            // Use Supabase PostgreSQL connection string for migrations
            optionsBuilder.UseNpgsql(
                "Host=db.ydmbjbhiasprzrexqkxy.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=7216021mikavdodo;SSL Mode=Require;Trust Server Certificate=true",
                b => b.MigrationsAssembly("WhatsApp.Shared"));

            return new SaaSDbContext(optionsBuilder.Options);
        }
    }
}
