using System.Reflection;
using Nova.Framework.Web.Modular;
using Xunit;

namespace Nova.ArchitectureTests;

/// <summary>
/// 模块自动发现契约：宿主在启动时会扫描 Nova.Modules.*.Api.dll 实例化 IModule，
/// 这里验证物理模块布局与命名约定始终保持完整。
/// </summary>
public class ModuleDiscoveryTests
{
    private static readonly string[] ExpectedModules =
    {
        "Agent", "Audit", "Billing", "Chat", "Identity", "Knowledge", "MCP",
        "Memory", "Model", "Multitenancy", "Notification", "Prompt", "Storage",
        "Tool", "Workflow", "Workspace"
    };

    [Fact]
    public void All_Sixteen_Modules_Are_Discovered_With_IModule()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directory = AppContext.BaseDirectory;

        foreach (var file in Directory.GetFiles(directory, "Nova.Modules.*.Api.dll"))
        {
            if (Path.GetFileName(file).Contains("Tests", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var assembly = Assembly.LoadFrom(file);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IModule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        if (Activator.CreateInstance(type) is IModule module)
                        {
                            found.Add(module.Name);
                        }
                    }
                }
            }
            catch
            {
                // 单个程序集加载失败时忽略，与宿主扫描行为一致
            }
        }

        var missing = ExpectedModules.Except(found, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Empty(missing);
        Assert.Equal(ExpectedModules.Length, found.Count);
    }
}
