using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.CQRS;
using Nova.Contracts.DependencyInjection;
using MassTransit;
using FluentValidation;

namespace Nova.Framework.Web.Modular;

public static class ModuleExtensions
{
    private static readonly List<IModule> RegisteredModules = new();

    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        var modules = DiscoverModules();
        
        var assemblies = new List<Assembly>();
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(directory, "Nova.*.dll");
        foreach (var file in dllFiles)
        {
            try { assemblies.Add(Assembly.LoadFrom(file)); } catch { }
        }

        // 1. Global Auto Dependency Injection (IScopedDependency, etc.)
        services.AddAutoDependencyInjection(assemblies.ToArray());

        // 2. Register FluentValidation validators
        services.AddValidatorsFromAssemblies(assemblies);

        // 3. Global MassTransit Mediator for all modules
        services.AddMediator(cfg =>
        {
            cfg.AddConsumers(assemblies.ToArray());
            cfg.ConfigureMediator((context, mediatorCfg) =>
            {
                mediatorCfg.UseConsumeFilter(typeof(ValidationFilter<>), context);
            });
        });

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
            RegisteredModules.Add(module);
            Console.WriteLine($"[Nova.Modules] Loaded module: {module.Name}");
        }

        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 3. Automatically map CQRS endpoints based on ApiEndpointAttribute (once for all modules)
        endpoints.MapAutoEndpoints();

        foreach (var module in RegisteredModules)
        {
            // 2. Map Module specific endpoints if any
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IEnumerable<IModule> DiscoverModules()
    {
        var modules = new List<IModule>();
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(directory, "Nova.Modules.*.Api.dll");

        foreach (var file in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    if (Activator.CreateInstance(type) is IModule moduleInstance)
                    {
                        modules.Add(moduleInstance);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nova.Modules] Failed to load assembly {file}: {ex.Message}");
            }
        }

        return modules;
    }
}
