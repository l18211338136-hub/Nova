using Microsoft.AspNetCore.Http;
using Nova.Contracts.Security;
using Nova.Framework.Web.Security;
using Nova.Framework.Web.Services;
using System.Security.Claims;
using Xunit;

namespace Nova.UnitTests.Framework.Web;

public class WebSecurityTests
{
    // .NET 10 中 EndpointFilterInvocationContext 为抽象类，这里提供一个最小具体实现用于测试。
    private class FakeEndpointContext : EndpointFilterInvocationContext
    {
        public FakeEndpointContext(HttpContext httpContext, IList<object?> arguments)
        {
            HttpContext = httpContext;
            Arguments = arguments;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; }

        public override T GetArgument<T>(int index) =>
            Arguments.Count > index ? (T)(object?)Arguments[index]! : default!;
    }

    private static EndpointFilterInvocationContext CreateContext(ClaimsPrincipal? user)
    {
        var http = new DefaultHttpContext();
        if (user is not null)
        {
            http.User = user;
        }

        return new FakeEndpointContext(http, new List<object?>());
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test-auth"));

    [Fact]
    public async Task PermissionFilter_Unauthenticated_Returns_Unauthorized()
    {
        var filter = new PermissionFilter("X.Y.Z");
        var context = CreateContext(null);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>("next"));

        var statusCode = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode;
        Assert.Equal(401, statusCode);
    }

    [Fact]
    public async Task PermissionFilter_Matching_Permission_Invokes_Next()
    {
        var user = Authenticated(new Claim("Permission", "X.Y.Z"));
        var filter = new PermissionFilter("X.Y.Z");
        var context = CreateContext(user);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>("next-called"));

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task PermissionFilter_Wildcard_Permission_Invokes_Next()
    {
        var user = Authenticated(new Claim("Permission", "*"));
        var filter = new PermissionFilter("X.Y.Z");
        var context = CreateContext(user);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>("next-called"));

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task PermissionFilter_Missing_Permission_Returns_Forbid()
    {
        var user = Authenticated(new Claim("Permission", "Other.Permission"));
        var filter = new PermissionFilter("X.Y.Z");
        var context = CreateContext(user);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>("next"));

        // .NET 10 的 ForbidHttpResult 不实现 IStatusCodeHttpResult，且 ExecuteAsync 需要认证服务。
        // 这里通过类型名断言其语义等价于 403 Forbidden。
        Assert.Equal("ForbidHttpResult", result?.GetType().Name);
    }

    [Fact]
    public void CurrentUser_Reads_Claims_From_HttpContext()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.Email, "alice@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "test-auth"));

        ICurrentUser currentUser = new CurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        });

        Assert.Equal(id, currentUser.Id);
        Assert.Equal("alice", currentUser.Name);
        Assert.Equal("alice@example.com", currentUser.Email);
        Assert.True(currentUser.IsAuthenticated);
        Assert.Contains("Admin", currentUser.Roles);
        Assert.True(currentUser.IsInRole("Admin"));
    }

    [Fact]
    public void CurrentUser_Without_HttpContext_Returns_Defaults()
    {
        ICurrentUser currentUser = new CurrentUser(new HttpContextAccessor());

        Assert.Null(currentUser.Id);
        Assert.False(currentUser.IsAuthenticated);
        Assert.Empty(currentUser.Roles);
    }
}
