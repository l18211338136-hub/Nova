using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nova.Framework.MultiTenancy.EntityFrameworkCore;

public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(GetConnectionString());
        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

        return CreateDbContext(optionsBuilder.Options);
    }

    protected abstract TContext CreateDbContext(DbContextOptions<TContext> options);

    private static string GetConnectionString()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var basePath = currentDir;
        
        // Find solution root
        while (basePath != null && !File.Exists(Path.Combine(basePath, "Nova.sln")))
        {
            basePath = Directory.GetParent(basePath)?.FullName;
        }

        basePath ??= currentDir;

        var appsettingsPath = Path.Combine(basePath, "src", "Host", "Nova.WebApi", "appsettings.json");
        if (!File.Exists(appsettingsPath))
        {
            // Fallback for execution within Migrator folder
            appsettingsPath = Path.Combine(currentDir, "..", "Nova.WebApi", "appsettings.json");
        }

        if (!File.Exists(appsettingsPath))
        {
            throw new FileNotFoundException($"Could not find appsettings.json at {appsettingsPath}");
        }

        var json = File.ReadAllText(appsettingsPath);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
    }
}
