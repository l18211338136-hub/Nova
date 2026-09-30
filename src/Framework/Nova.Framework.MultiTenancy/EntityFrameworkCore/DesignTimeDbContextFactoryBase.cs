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
        var env = GetEnvironmentName();

        // 解析 appsettings 所在目录。优先级：
        //   1) 程序集同目录（dotnet publish / 运行 / dotnet ef 后配置文件随 DLL 发布到这里）
        //   2) 当前工作目录（dotnet run 时通常是项目目录）
        //   3) 源码树：从当前目录向上找 Nova.sln，再定位 src/Host/Nova.WebApi
        // 命中条件：该目录存在「基础 appsettings.json」或「环境专属 appsettings.{env}.json」。
        // 基础文件改为可选，以支持生产服务器仅部署 appsettings.Production.json。
        var resolvedBase = TryFindConfigBase(env);
        if (resolvedBase == null)
        {
            throw new InvalidOperationException(
                $"无法定位 appsettings 配置文件以解析设计期连接串（{ConnectionStringName}）。" +
                "期望基础 appsettings.json 或 appsettings.{Environment}.json 位于程序集同目录、" +
                "当前工作目录，或源码树的 src/Host/Nova.WebApi。");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(resolvedBase)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not found in configuration.");
    }

    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
    }

    private static string? TryFindConfigBase(string env)
    {
        bool HasConfig(string dir) =>
            Directory.Exists(dir) &&
            (File.Exists(Path.Combine(dir, "appsettings.json")) ||
             File.Exists(Path.Combine(dir, $"appsettings.{env}.json")));

        // 1) 发布 / 运行 / dotnet ef：配置文件与程序集（DLL）同目录
        var baseDir = AppContext.BaseDirectory;
        if (HasConfig(baseDir))
        {
            return baseDir;
        }

        // 2) 当前工作目录（dotnet run 时一般为项目目录）
        var cwd = Directory.GetCurrentDirectory();
        if (HasConfig(cwd))
        {
            return cwd;
        }

        // 3) 源码树：向上找 Nova.sln，再定位 src/Host/Nova.WebApi
        var current = cwd;
        while (current != null && !File.Exists(Path.Combine(current, "Nova.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }

        if (current != null)
        {
            var webApiPath = Path.Combine(current, "src", "Host", "Nova.WebApi");
            if (HasConfig(webApiPath))
            {
                return webApiPath;
            }
        }

        return null;
    }
}

