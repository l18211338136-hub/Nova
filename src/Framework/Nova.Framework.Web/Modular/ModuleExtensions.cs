using FluentValidation;
using MassTransit;
using MassTransit.Clients;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Authorization;
using Nova.Framework.Web.CQRS;
using System.Reflection;

namespace Nova.Framework.Web.Modular;

public static class ModuleExtensions
{
    private static readonly List<IModule> RegisteredModules = new();

    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger("Nova.Modules");

        var modules = DiscoverModules(logger);

        var assemblies = new List<Assembly>();
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(directory, "Nova.*.dll");

        var mvcBuilder = services.AddMvcCore();

        foreach (var file in dllFiles)
        {
            try
            {
                var asm = Assembly.LoadFrom(file);
                assemblies.Add(asm);
                mvcBuilder.AddApplicationPart(asm);
            }
            catch { }
        }

        services.AddAutoDependencyInjection(assemblies.ToArray());
        services.AddValidatorsFromAssemblies(assemblies);

        // 让模块 MVC Controller 上的 [RequirePermission] 真正生效
        // （PermissionFilter/IEndpointFilter 只覆盖声明式端点，MVC 走这条全局过滤器）
        mvcBuilder.AddMvcOptions(options => options.Filters.Add<RequirePermissionActionFilter>());
        services.AddMediator(cfg =>
        {
            cfg.AddConsumers(assemblies.ToArray());
            cfg.ConfigureMediator((context, mediatorCfg) =>
            {
                mediatorCfg.UseConsumeFilter(typeof(ValidationFilter<>), context);

                // 动态注册 InboxFilter 以实现收件箱幂等去重
                var inboxFilterType = Type.GetType("Nova.Framework.EventBus.Outbox.InboxFilter`1, Nova.Framework.EventBus");
                if (inboxFilterType != null)
                {
                    mediatorCfg.UseConsumeFilter(inboxFilterType, context);
                }
            });
        });

        // AddMediator 不会注册 IScopedClientFactory（那是 AddMassTransit 带真实总线时才注册的），
        // 而 NovaControllerBase / McpControllerBase 依赖它做 Request/Response。
        // IScopedMediator 本身实现了 IClientFactory，这里补一个 Scoped 注册
        // （等价于 MassTransit Mediator 注册器 GetScopedBusContext 的无 ConsumeContext 分支）。
        services.TryAddScoped<IScopedClientFactory>(provider =>
            new ScopedClientFactory(provider.GetRequiredService<IScopedMediator>(), null!));

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
            RegisteredModules.Add(module);
            logger.LogInformation("[Nova.Modules] Loaded module: {ModuleName}", module.Name);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 3. Automatically map CQRS endpoints based on ApiEndpointAttribute (once for all modules)
        endpoints.MapAutoEndpoints();

        // Map MVC controllers from module assemblies (e.g. MCP module's McpController).
        // AddModules 已通过 AddMvcCore + AddApplicationPart 注册了控制器，
        // 但若不调用 MapControllers，这些路由不会进入路由表（OpenAPI 里却能看到，极易误判）。
        endpoints.MapControllers();

        foreach (var module in RegisteredModules)
        {
            // 2. Map Module specific endpoints if any
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IEnumerable<IModule> DiscoverModules(ILogger logger)
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
                logger.LogError(ex, "[Nova.Modules] Failed to load assembly {AssemblyFile}", file);
            }
        }

        return modules;
    }
}
