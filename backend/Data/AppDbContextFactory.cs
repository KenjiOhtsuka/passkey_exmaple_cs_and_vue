using backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Data;

/// <summary>
/// Design-time factory used to generate EF Core migrations for the Azure Functions host.
/// It replicates the Identity schema version (3.0) configured at runtime in Program.cs.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        });
        services.AddEntityFrameworkSqlite();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=passkeys.db")
            .UseApplicationServiceProvider(services.BuildServiceProvider())
            .Options;

        return new AppDbContext(options);
    }
}