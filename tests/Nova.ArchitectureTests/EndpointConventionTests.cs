using System.Reflection;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;
using Xunit;

namespace Nova.ArchitectureTests;

/// <summary>
/// CQRS 声明式端点约定：所有带 [ApiEndpoint] 的命令都应是良构的，且声明的权限非空。
/// </summary>
public class EndpointConventionTests
{
    private static IEnumerable<Type> LoadCommandTypes()
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

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ApiEndpointAttribute>() is not null)
                {
                    yield return type;
                }
            }
        }
    }

    [Fact]
    public void ApiEndpoint_Commands_Have_Well_Formed_Routes()
    {
        foreach (var type in LoadCommandTypes())
        {
            var attribute = type.GetCustomAttribute<ApiEndpointAttribute>()!;

            Assert.StartsWith("/api", attribute.Route, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(attribute.Method));
            Assert.NotNull(attribute.ResponseType);
        }
    }

    [Fact]
    public void Declared_Permissions_Are_NonEmpty()
    {
        foreach (var type in LoadCommandTypes())
        {
            var permission = type.GetCustomAttribute<RequirePermissionAttribute>();
            if (permission is null)
            {
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(permission.Permission),
                $"{type.FullName} 声明了空的 RequirePermission");
        }
    }
}
