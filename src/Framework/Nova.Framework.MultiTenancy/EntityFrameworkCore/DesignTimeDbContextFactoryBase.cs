using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Nova.Framework.MultiTenancy.EntityFrameworkCore;

public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// 设计期/启动自动迁移使用的连接串名。默认 "DefaultConnection"；
    /// 需要让某个模块落到独立数据库的工厂（如 McpDbContextFactory）可覆盖此属性。
    /// </summary>
    protected virtual string ConnectionStringName => "DefaultConnection";

    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(GetConnectionString());
        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

        return CreateDbContext(optionsBuilder.Options);
    }

    protected abstract TContext CreateDbContext(DbContextOptions<TContext> options);

    private string GetConnectionString()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var basePath = currentDir;
        
        // Find solution root
        while (basePath != null && !File.Exists(Path.Combine(basePath, "Nova.sln")))
        {
            basePath = Directory.GetParent(basePath)?.FullName;
        }

        basePath ??= currentDir;

        var webApiPath = Path.Combine(basePath, "src", "Host", "Nova.WebApi");
        if (!Directory.Exists(webApiPath))
        {
            // Fallback for execution within Migrator folder
            webApiPath = Path.Combine(currentDir, "..", "Nova.WebApi");
        }

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not found in configuration.");
    }
}

