using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Nova.Framework.Persistence.Outbox;

public class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var basePath = currentDir;
        
        while (basePath != null && !File.Exists(Path.Combine(basePath, "Nova.sln")))
        {
            basePath = Directory.GetParent(basePath)?.FullName;
        }
        basePath ??= currentDir;

        var webApiPath = Path.Combine(basePath, "src", "Host", "Nova.WebApi");
        if (!Directory.Exists(webApiPath))
        {
            webApiPath = Path.Combine(currentDir, "..", "Nova.WebApi");
        }

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not found.");

        var builder = new DbContextOptionsBuilder<OutboxDbContext>();
        builder.UseNpgsql(connectionString);

        var dummyTenant = new DummyTenantInfo { Id = "dummy", Identifier = "dummy", Name = "dummy" };
        return new OutboxDbContext(builder.Options, dummyTenant);
    }

    private class DummyTenantInfo : Finbuckle.MultiTenant.Abstractions.ITenantInfo
    {
        public string? Id { get; set; }
        public string? Identifier { get; set; }
        public string? Name { get; set; }
    }
}
