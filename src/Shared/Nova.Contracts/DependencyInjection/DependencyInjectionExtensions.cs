using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Contracts.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAutoDependencyInjection(this IServiceCollection services, Assembly[] assemblies)
    {
        var transientType = typeof(ITransientDependency);
        var scopedType = typeof(IScopedDependency);
        var singletonType = typeof(ISingletonDependency);

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);
            foreach (var type in types)
            {
                if (transientType.IsAssignableFrom(type))
                {
                    services.AddTransient(type);
                    var interfaces = type.GetInterfaces().Where(i => i != transientType);
                    foreach (var i in interfaces) services.AddTransient(i, type);
                }
                else if (scopedType.IsAssignableFrom(type))
                {
                    services.AddScoped(type);
                    var interfaces = type.GetInterfaces().Where(i => i != scopedType);
                    foreach (var i in interfaces) services.AddScoped(i, type);
                }
                else if (singletonType.IsAssignableFrom(type))
                {
                    services.AddSingleton(type);
                    var interfaces = type.GetInterfaces().Where(i => i != singletonType);
                    foreach (var i in interfaces) services.AddSingleton(i, type);
                }
            }
        }
        return services;
    }
}
