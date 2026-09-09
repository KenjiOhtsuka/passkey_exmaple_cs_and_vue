using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Strip the default "AspNet" prefix from the Identity table names
        // (AspNetUsers -> Users, AspNetUserPasskeys -> UserPasskeys, ...).
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null && tableName.StartsWith("AspNet", StringComparison.Ordinal))
            {
                entity.SetTableName(tableName["AspNet".Length..]);
            }
        }
    }
}