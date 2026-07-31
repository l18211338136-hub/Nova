using System.Reflection;
using Nova.Contracts.CQRS;
using Nova.Contracts.Security;
using Nova.Modules.Identity.Application.Users.Commands;
using Xunit;

namespace Nova.UnitTests.Contracts;

public class ContractAttributeTests
{
    [Fact]
    public void CreateUserCommand_Declares_Endpoint_And_Permission()
    {
        var endpoint = typeof(CreateUserCommand).GetCustomAttribute<ApiEndpointAttribute>();
        var permission = typeof(CreateUserCommand).GetCustomAttribute<RequirePermissionAttribute>();

        Assert.NotNull(endpoint);
        Assert.Equal("POST", endpoint!.Method);
        Assert.Equal("/api/identity/users", endpoint.Route);
        Assert.Equal(typeof(CreateUserResult), endpoint.ResponseType);

        Assert.NotNull(permission);
        Assert.Equal("Identity.Users.Create", permission!.Permission);
    }

    [Fact]
    public void RegisterUserCommand_Is_Anonymous_Without_Permission()
    {
        var endpoint = typeof(RegisterUserCommand).GetCustomAttribute<ApiEndpointAttribute>();
        var permission = typeof(RegisterUserCommand).GetCustomAttribute<RequirePermissionAttribute>();

        Assert.NotNull(endpoint);
        Assert.Equal("POST", endpoint!.Method);
        Assert.Equal("/api/identity/register", endpoint.Route);

        // 注册是公开接口，不应要求登录权限
        Assert.Null(permission);
    }
}
