using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Middlewares;
using Nova.Framework.Web.Responses;
using Nova.Modules.Identity.Application.Users.Commands;
using Nova.Modules.Identity.Domain.Users;
using Nova.UnitTests.Fakes;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Nova.Framework.Domain.SeedWork;
using Xunit;

namespace Nova.UnitTests.Handlers;

public class BugFixRound2Tests
{
    private class TestMultiTenantContext : Finbuckle.MultiTenant.Abstractions.IMultiTenantContext<NovaTenantInfo>
    {
        public NovaTenantInfo? TenantInfo { get; set; }
        Finbuckle.MultiTenant.Abstractions.ITenantInfo? Finbuckle.MultiTenant.Abstractions.IMultiTenantContext.TenantInfo
        {
            get => TenantInfo;
            init => TenantInfo = (NovaTenantInfo?)value;
        }
        public Finbuckle.MultiTenant.Abstractions.StrategyInfo? StrategyInfo { get; init; }
        public Finbuckle.MultiTenant.Abstractions.StoreInfo<NovaTenantInfo>? StoreInfo { get; set; }
        public bool IsResolved => true;
    }

    // ── BUG-13 验证 TOTP 密码重置后 SecurityStamp 是否更新 ─────────────

    [Fact]
    public async Task ResetPassword_Should_Update_SecurityStamp_To_Prevent_Token_Replay()
    {
        // Arrange
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = User.Create("totp-test@example.com", "totp-test@example.com");
        await userManager.CreateAsync(user, "OldPass@123");

        var initialStamp = user.SecurityStamp;

        var code = await userManager.GenerateTwoFactorTokenAsync(user, "Email");

        var handler = new ResetPasswordCommandHandler(userManager);
        var command = new ResetPasswordCommand
        {
            Email = "totp-test@example.com",
            Code = code,
            NewPassword = "NewPass@123"
        };
        var context = HandlerTestHarness.CreateConsumeContext(command);

        // Act
        await handler.Consume(context);

        // Assert
        var updatedUser = await userManager.FindByEmailAsync("totp-test@example.com");
        Assert.NotNull(updatedUser);
        Assert.NotEqual(initialStamp, updatedUser!.SecurityStamp);

        var result = IdentityIntegrationHarness.GetResponded<ResetPasswordResult>(context);
        Assert.NotNull(result);
        Assert.True(result!.Success);
    }

    // ── BUG-14 & BUG-20 验证全局异常中间件的敏感信息脱敏与响应码 ─────

    [Fact]
    public async Task GlobalExceptionMiddleware_Should_Sanitize_Unhandled_Exception_Messages()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("DB Connection String: Host=secret;Pass=123456");
        var logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(next, logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(500, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("123456", json);
        Assert.Contains("服务器内部错误，请稍后再试。", json);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_Should_Return_BadRequest_For_NovaValidationException()
    {
        // Arrange
        RequestDelegate next = _ => throw new NovaValidationException("业务验证未通过");
        var logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(next, logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(400, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.Contains("业务验证未通过", json);
    }

    // ── BUG-18 验证 ValidUpto 为 default 时租户守卫不拦截 ─────────────

    [Fact]
    public async Task TenantGuardMiddleware_Should_Not_Block_Tenant_With_Default_ValidUpto()
    {
        // Arrange
        var isNextCalled = false;
        RequestDelegate next = _ =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantGuardMiddleware(next);
        var httpContext = new DefaultHttpContext();

        var tenantInfo = new NovaTenantInfo
        {
            Id = "new-tenant",
            Identifier = "new-tenant",
            IsActive = true,
            ValidUpto = default // 未设置过期时间
        };

        var accessor = Substitute.For<Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor<NovaTenantInfo>>();
        accessor.MultiTenantContext.Returns(new TestMultiTenantContext { TenantInfo = tenantInfo });

        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.True(isNextCalled);
        Assert.NotEqual(403, httpContext.Response.StatusCode);
    }

    // ── JWT AccessToken 签名防防篡改校验 ─────────────────────────────

    [Fact]
    public async Task RefreshToken_WithTamperedSignature_ThrowsNovaValidationException()
    {
        // Arrange
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var tenantDb = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();

        var handler = new RefreshTokenCommandHandler(scopeFactory, tenantDb, dispatcher, config);

        // 构造一个签名被篡改的假 Token 字符串
        var tamperedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.tampered_signature_string";

        var context = HandlerTestHarness.CreateConsumeContext(new RefreshTokenCommand
        {
            AccessToken = tamperedToken,
            RefreshToken = "some-refresh-token"
        });

        // Act & Assert
        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(context));
    }
}
