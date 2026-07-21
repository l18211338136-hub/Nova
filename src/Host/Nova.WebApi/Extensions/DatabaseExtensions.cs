using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nova.Modules.Identity.Domain;
using Nova.Framework.MultiTenancy;
using Finbuckle.MultiTenant.Abstractions;

namespace Nova.WebApi.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this IApplicationBuilder app)
    {
        Console.WriteLine("[Nova.Database] Starting automatic migrations...");
        
        // Ensure all Nova assemblies are loaded into the AppDomain
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(directory, "Nova.*.dll");
        foreach (var file in dllFiles)
        {
            try { Assembly.LoadFrom(file); } catch { }
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var factoryTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface && 
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDesignTimeDbContextFactory<>)))
            .ToList();

        if (!factoryTypes.Any())
        {
            Console.WriteLine("[Nova.Database] No IDesignTimeDbContextFactory found.");
            return;
        }

        foreach (var factoryType in factoryTypes)
        {
            try
            {
                var factory = Activator.CreateInstance(factoryType);
                var method = factoryType.GetMethod("CreateDbContext");
                if (method != null)
                {
                    if (method.Invoke(factory, [Array.Empty<string>()]) is DbContext dbContext)
                    {
                        Console.WriteLine($"[Nova.Database] Migrating {dbContext.GetType().Name}...");
                        await dbContext.Database.MigrateAsync();
                        await dbContext.DisposeAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nova.Database] Failed to migrate using {factoryType.Name}: {ex.Message}");
            }
        }
        
        Console.WriteLine("[Nova.Database] Automatic migrations completed.");

        // === Execute Data Initializers for Root Tenant ===
        Console.WriteLine("[Nova.Database] Checking and seeding Root tenant...");
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<NovaTenantInfo>>();
            
            var rootTenantId = NovaIdentityConstants.Tenants.RootTenantId;
            var rootTenant = await tenantStore.GetAsync(rootTenantId);
            if (rootTenant == null)
            {
                rootTenant = new NovaTenantInfo
                {
                    Id = rootTenantId,
                    Identifier = rootTenantId,
                    Name = NovaIdentityConstants.Tenants.RootTenantName,
                    AdminEmail = NovaIdentityConstants.Seed.RootEmail,
                    IsActive = true,
                    ValidUpto = DateTime.UtcNow.AddYears(100)
                };
                await tenantStore.AddAsync(rootTenant);
                Console.WriteLine("[Nova.Database] Root tenant created.");
            }

            var contextSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            contextSetter.MultiTenantContext = new ManualTenantContext { TenantInfo = rootTenant };

            var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
            foreach (var initializer in initializers)
            {
                Console.WriteLine($"[Nova.Database] Executing seeders for {initializer.GetType().Name}...");
                await initializer.MigrateAsync(default);
                await initializer.SeedAsync(default);
            }
            Console.WriteLine("[Nova.Database] Root tenant seeding completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Nova.Database] Failed to seed Root tenant: {ex.Message}");
        }
    }
}

public class ManualTenantContext : IMultiTenantContext<NovaTenantInfo>
{
    public NovaTenantInfo? TenantInfo { get; set; }
    ITenantInfo? IMultiTenantContext.TenantInfo { get => TenantInfo; init => TenantInfo = (NovaTenantInfo?)value; }
    public StrategyInfo? StrategyInfo { get; init; }
    public StoreInfo<NovaTenantInfo>? StoreInfo { get; set; }
    public bool IsResolved => true;
}
