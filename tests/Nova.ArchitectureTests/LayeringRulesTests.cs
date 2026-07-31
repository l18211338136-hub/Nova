
using System.Reflection;

namespace Nova.ArchitectureTests;

/// <summary>
/// 架构分层约束：确保整洁架构的依赖方向不被破坏。
/// </summary>
public class LayeringRulesTests
{
    private static IEnumerable<Assembly> LoadNovaAssemblies()
    {
        var directory = AppContext.BaseDirectory;
        foreach (var file in Directory.GetFiles(directory, "Nova.*.dll"))
        {
            if (file.Contains("Tests", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(file);
            }
            catch
            {
                continue;
            }

            yield return assembly;
        }
    }

    [Fact]
    public void Domain_Layers_Must_Not_Reference_Higher_Layers()
    {
        // 领域层只能依赖 Framework / Contracts，绝不能反向依赖 Application / Infrastructure / Api
        var forbiddenSuffixes = new[] { ".Application", ".Infrastructure", ".Api" };
        var violations = new List<string>();

        foreach (var assembly in LoadNovaAssemblies())
        {
            if (!assembly.GetName().Name!.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var dependency in assembly.GetReferencedAssemblies())
            {
                var name = dependency.Name!;
                if (!name.StartsWith("Nova.Modules", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (forbiddenSuffixes.Any(s => name.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"{assembly.GetName().Name} -> {name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_Module_Api_Implements_IModule()
    {
        var moduleType = typeof(Nova.Framework.Web.Modular.IModule);
        var missing = new List<string>();

        foreach (var assembly in LoadNovaAssemblies())
        {
            var name = assembly.GetName().Name!;
            if (!name.StartsWith("Nova.Modules", StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(".Api", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var implementsModule = assembly.GetTypes()
                .Any(t => moduleType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (!implementsModule)
            {
                missing.Add(name);
            }
        }

        Assert.Empty(missing);
    }
}
