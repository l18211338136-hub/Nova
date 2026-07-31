using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Commands;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Roles.Commands;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Application.Users.Commands;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Infrastructure;
using NSubstitute;
using Xunit;

namespace Nova.UnitTests.Handlers;

/// <summary>
/// B 档重依赖 Handler 集成测试：用真实 ASP.NET Identity + EF InMemory + Finbuckle + MassTransit Mediator 桩，
/// 覆盖 Login / CreateUser / RefreshToken / EmailLogin / Send*Code / Role / RegisterUser 的核心路径。
/// </summary>
public class BTrackHandlerTests
{
    private const string DefaultPassword = "Pass@123";

    #region Seed helpers

    private static async Task<User> SeedUserAsync(IdentityIntegrationHarness harness, NovaTenantInfo tenant, string email, string userName, string password)
    {
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider, tenant);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = User.Create(userName, email);
        var r = await um.CreateAsync(user, password);
        if (!r.Succeeded)
        {
            throw new InvalidOperationException("SeedUser failed: " + string.Join(", ", r.Errors.Select(e => e.Description)));
        }
        return user;
    }

    /// <summary>
    /// 在同一 scope 内创建用户并写入刷新令牌，避免跨 scope 传递 User 实例导致的 change-tracker 冲突；
    /// 同时生成可解析的 access token 供 RefreshToken 测试使用。
    /// </summary>
    private static async Task<(User user, string accessToken)> SeedUserWithRefreshTokenAsync(
        IdentityIntegrationHarness harness, NovaTenantInfo tenant, string email, string userName, string password, string refreshToken)
    {
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider, tenant);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = User.Create(userName, email);
        var cr = await um.CreateAsync(user, password);
        if (!cr.Succeeded)
        {
            throw new InvalidOperationException("SeedUser failed: " + string.Join(", ", cr.Errors.Select(e => e.Description)));
        }
        await um.SetAuthenticationTokenAsync(user, "NovaApp", "RefreshToken", $"{refreshToken}|{DateTimeOffset.UtcNow.AddDays(7):O}");
        var ts = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var accessToken = ts.GenerateToken(user, tenant.Identifier).Token;
        return (user, accessToken);
    }

    private static async Task SeedTenantInfoAsync(IdentityIntegrationHarness harness, NovaTenantInfo tenant)
    {
        using var scope = harness.CreateScope();
        // 系统库（TestTenantDbContext）同样受 Finbuckle 多租户强制约束，写入前需设置租户上下文
        harness.SetTenant(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        db.TenantInfo.Add(tenant);
        await db.SaveChangesAsync();
    }

    private static async Task SeedMappingAsync(IdentityIntegrationHarness harness, string account, string tenantId)
    {
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        db.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping { Account = account, TenantId = tenantId });
        await db.SaveChangesAsync();
    }

    #endregion

    #region Role handlers

    [Fact]
    public async Task CreateRole_DuplicateName_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        await rm.CreateAsync(Role.Create("Admin", "管理员", null, 0));

        var handler = new CreateRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new CreateRoleCommand
        {
            Name = "Admin",
            DisplayName = "管理员",
            Sort = 0,
            IsEnabled = true
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task CreateRole_Success_ReturnsRoleId()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        var handler = new CreateRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new CreateRoleCommand
        {
            Name = "Editor",
            DisplayName = "编辑",
            Remarks = "r",
            Sort = 1,
            IsEnabled = true,
            Permissions = new() { "Permission.A", "Permission.B" }
        });

        await handler.Consume(ctx);

        var result = IdentityIntegrationHarness.GetResponded<CreateRoleResult>(ctx);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.RoleId);

        var created = await rm.FindByIdAsync(result.RoleId.ToString());
        Assert.NotNull(created);
        var claims = await rm.GetClaimsAsync(created!);
        Assert.Contains(claims, c => c.Type == "Permission" && c.Value == "Permission.A");
    }

    [Fact]
    public async Task UpdateRole_NotFound_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        var handler = new UpdateRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new UpdateRoleCommand
        {
            Id = Guid.NewGuid(),
            Name = "X",
            DisplayName = "X",
            Sort = 0,
            IsEnabled = true
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task UpdateRole_Success_UpdatesFields()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var role = Role.Create("Editor", "编辑", null, 0);
        await rm.CreateAsync(role);

        var handler = new UpdateRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new UpdateRoleCommand
        {
            Id = role.Id,
            Name = "Editor2",
            DisplayName = "编辑2",
            Sort = 5,
            IsEnabled = false,
            Permissions = new() { "Permission.C" }
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<UpdateRoleResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);

        var updated = await rm.FindByIdAsync(role.Id.ToString());
        Assert.Equal("Editor2", updated!.Name);
        Assert.False(updated.IsEnabled);
    }

    [Fact]
    public async Task DeleteRole_NotFound_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        var handler = new DeleteRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new DeleteRoleCommand { Id = Guid.NewGuid() });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task DeleteRole_Success_ReturnsTrue()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var role = Role.Create("Temp", "临时", null, 0);
        await rm.CreateAsync(role);

        var handler = new DeleteRoleCommandHandler(rm);
        var ctx = HandlerTestHarness.CreateConsumeContext(new DeleteRoleCommand { Id = role.Id });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<DeleteRoleResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);
        Assert.Null(await rm.FindByIdAsync(role.Id.ToString()));
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_DuplicateEmail_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        await SeedUserAsync(harness, harness.CurrentTenant, "dup@test.com", "dupuser", DefaultPassword);

        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new CreateUserCommandHandler(um, tenantDb, harness.CurrentTenant);
        var ctx = HandlerTestHarness.CreateConsumeContext(new CreateUserCommand
        {
            UserName = "newuser",
            Email = "dup@test.com",
            Password = DefaultPassword
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task CreateUser_DuplicateUserName_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        await SeedUserAsync(harness, harness.CurrentTenant, "a@test.com", "taken", DefaultPassword);

        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new CreateUserCommandHandler(um, tenantDb, harness.CurrentTenant);
        var ctx = HandlerTestHarness.CreateConsumeContext(new CreateUserCommand
        {
            UserName = "taken",
            Email = "b@test.com",
            Password = DefaultPassword
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task CreateUser_Success_CreatesUserAndMapping()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        // 确保目标租户下 Admin 角色存在，否则 AddToRolesAsync 会抛 "Role ADMIN does not exist"
        if (!await rm.RoleExistsAsync("Admin"))
        {
            await rm.CreateAsync(Role.Create("Admin", "管理员", null, 0));
        }
        var tenantDb = scope.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new CreateUserCommandHandler(um, tenantDb, harness.CurrentTenant);
        var ctx = HandlerTestHarness.CreateConsumeContext(new CreateUserCommand
        {
            UserName = "alice",
            Email = "alice@test.com",
            Password = DefaultPassword,
            PhoneNumber = "13800000000",
            Roles = new() { "Admin" },
            Permissions = new() { "Permission.X" }
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<CreateUserResult>(ctx);
        Assert.NotNull(resp);
        Assert.NotEqual(Guid.Empty, resp!.UserId);

        var created = await um.FindByEmailAsync("alice@test.com");
        Assert.NotNull(created);
        Assert.True(await um.IsInRoleAsync(created!, "Admin"));
        var claims = await um.GetClaimsAsync(created!);
        Assert.Contains(claims, c => c.Type == "Permission" && c.Value == "Permission.X");

        // 全局映射表应写入当前租户
        var mappingExists = (await tenantDb.GlobalUserTenantMappings
            .Where(m => m.Account == "alice@test.com" && m.TenantId == harness.CurrentTenant.Identifier)
            .ToListAsync()).Any();
        Assert.True(mappingExists);
    }

    #endregion

    #region Login / RefreshToken / EmailLogin

    [Fact]
    public async Task Login_InvalidAccount_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new LoginCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new LoginCommand
        {
            Account = "nobody@test.com",
            Password = DefaultPassword
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task Login_SingleTenant_Success_ReturnsToken()
    {
        var harness = IdentityIntegrationHarness.Create();
        var tenant = harness.CurrentTenant;
        await SeedTenantInfoAsync(harness, tenant);
        await SeedMappingAsync(harness, "user@test.com", tenant.Identifier);
        await SeedUserAsync(harness, tenant, "user@test.com", "user@test.com", DefaultPassword);

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new LoginCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new LoginCommand
        {
            Account = "user@test.com",
            Password = DefaultPassword
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<LoginResult>(ctx);
        Assert.NotNull(resp);
        Assert.False(string.IsNullOrEmpty(resp!.Token));
        Assert.False(string.IsNullOrEmpty(resp.RefreshToken));
        Assert.False(resp.RequiresTenantSelection);
    }

    [Fact]
    public async Task Login_MultipleTenants_ReturnsSelection()
    {
        var harness = IdentityIntegrationHarness.Create();
        var t1 = new NovaTenantInfo { Id = "t1", Identifier = "t1", Name = "T1", ConnectionString = "x", IsActive = true };
        var t2 = new NovaTenantInfo { Id = "t2", Identifier = "t2", Name = "T2", ConnectionString = "x", IsActive = true };
        await SeedTenantInfoAsync(harness, t1);
        await SeedTenantInfoAsync(harness, t2);
        await SeedMappingAsync(harness, "multi@test.com", "t1");
        await SeedMappingAsync(harness, "multi@test.com", "t2");
        await SeedUserAsync(harness, t1, "multi@test.com", "multi@test.com", DefaultPassword);
        await SeedUserAsync(harness, t2, "multi@test.com", "multi@test.com", DefaultPassword);

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new LoginCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new LoginCommand
        {
            Account = "multi@test.com",
            Password = DefaultPassword
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<LoginResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.RequiresTenantSelection);
        Assert.NotNull(resp.AvailableTenants);
        Assert.Equal(2, resp.AvailableTenants!.Count);
    }

    [Fact]
    public async Task RefreshToken_InvalidAccessTokenFormat_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new RefreshTokenCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new RefreshTokenCommand
        {
            AccessToken = "not-a-jwt",
            RefreshToken = "whatever"
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task RefreshToken_MismatchedRefreshToken_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        var tenant = harness.CurrentTenant;
        await SeedTenantInfoAsync(harness, tenant);
        // 同一 scope 内创建用户 + 写刷新令牌 + 生成 access token，规避跨 scope 的 change-tracker 冲突
        var (_, accessToken) = await SeedUserWithRefreshTokenAsync(
            harness, tenant, "rt@test.com", "rt@test.com", DefaultPassword, "stored-rt");

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new RefreshTokenCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new RefreshTokenCommand
        {
            AccessToken = accessToken,
            RefreshToken = "wrong-rt"
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task RefreshToken_Success_RotatesToken()
    {
        var harness = IdentityIntegrationHarness.Create();
        var tenant = harness.CurrentTenant;
        await SeedTenantInfoAsync(harness, tenant);

        const string storedRt = "stored-rt-123";
        var (_, accessToken) = await SeedUserWithRefreshTokenAsync(
            harness, tenant, "rt2@test.com", "rt2@test.com", DefaultPassword, storedRt);

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new RefreshTokenCommandHandler(scopeFactory, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new RefreshTokenCommand
        {
            AccessToken = accessToken,
            RefreshToken = storedRt
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<LoginResult>(ctx);
        Assert.NotNull(resp);
        Assert.False(string.IsNullOrEmpty(resp!.Token));
        Assert.NotEqual(storedRt, resp.RefreshToken);
    }

    [Fact]
    public async Task EmailLogin_InvalidCode_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        var tenant = harness.CurrentTenant;
        await SeedTenantInfoAsync(harness, tenant);
        await SeedMappingAsync(harness, "email@test.com", tenant.Identifier);
        await SeedUserAsync(harness, tenant, "email@test.com", "email@test.com", DefaultPassword);

        var cache = new FakeNovaCache();
        await cache.SetAsync("LoginCode:email@test.com", "999999");

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new EmailLoginCommandHandler(scopeFactory, tenantDb, cache);
        var ctx = HandlerTestHarness.CreateConsumeContext(new EmailLoginCommand
        {
            Email = "email@test.com",
            Code = "000000"
        });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task EmailLogin_ValidCode_Success()
    {
        var harness = IdentityIntegrationHarness.Create();
        var tenant = harness.CurrentTenant;
        await SeedTenantInfoAsync(harness, tenant);
        await SeedMappingAsync(harness, "email2@test.com", tenant.Identifier);
        await SeedUserAsync(harness, tenant, "email2@test.com", "email2@test.com", DefaultPassword);

        var cache = new FakeNovaCache();
        await cache.SetAsync("LoginCode:email2@test.com", "123456");

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new EmailLoginCommandHandler(scopeFactory, tenantDb, cache);
        var ctx = HandlerTestHarness.CreateConsumeContext(new EmailLoginCommand
        {
            Email = "email2@test.com",
            Code = "123456"
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<LoginResult>(ctx);
        Assert.NotNull(resp);
        Assert.False(string.IsNullOrEmpty(resp!.Token));

        // 验证码消费后应被清除
        Assert.Null(await cache.GetAsync<string>("LoginCode:email2@test.com"));
    }

    #endregion

    #region Send*Code

    [Fact]
    public async Task SendEmailLoginCode_UnknownEmail_ReturnsSuccessWithoutEnumeration()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<IMediator>();
        var cache = new FakeNovaCache();

        var handler = new SendEmailLoginCodeCommandHandler(tenantDb, mediator, cache);
        var ctx = HandlerTestHarness.CreateConsumeContext(new SendEmailLoginCodeCommand { Email = "ghost@test.com" });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<SendEmailLoginCodeResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);
        // 未知邮箱不应写入任何验证码
        Assert.Null(await cache.GetAsync<string>("LoginCode:ghost@test.com"));
    }

    [Fact]
    public async Task SendEmailLoginCode_KnownEmail_CachesCode()
    {
        var harness = IdentityIntegrationHarness.Create();
        await SeedMappingAsync(harness, "known@test.com", harness.CurrentTenant.Identifier);
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<IMediator>();
        var cache = new FakeNovaCache();

        var handler = new SendEmailLoginCodeCommandHandler(tenantDb, mediator, cache);
        var ctx = HandlerTestHarness.CreateConsumeContext(new SendEmailLoginCodeCommand { Email = "known@test.com" });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<SendEmailLoginCodeResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);
        Assert.False(string.IsNullOrEmpty(await cache.GetAsync<string>("LoginCode:known@test.com")));
        // 验证邮件应通过 Mediator 发出
        await mediator.Received().Send(Arg.Any<SendEmailCommand>());
    }

    [Fact]
    public async Task SendEmailRegisterCode_EmailTaken_Throws()
    {
        var harness = IdentityIntegrationHarness.Create();
        await SeedMappingAsync(harness, "taken@test.com", NovaIdentityConstants.Tenants.RetailTenantId);
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<IMediator>();
        var cache = new FakeNovaCache();

        var handler = new SendEmailRegisterCodeCommandHandler(mediator, cache, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new SendEmailRegisterCodeCommand { Email = "taken@test.com" });

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task SendEmailRegisterCode_Success_CachesCode()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<IMediator>();
        var cache = new FakeNovaCache();

        var handler = new SendEmailRegisterCodeCommandHandler(mediator, cache, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new SendEmailRegisterCodeCommand { Email = "fresh@test.com" });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<SendEmailRegisterCodeResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);
        Assert.False(string.IsNullOrEmpty(await cache.GetAsync<string>("RegisterCode:fresh@test.com")));
    }

    [Fact]
    public async Task SendForgotPasswordCode_UserNotFound_ReturnsSuccess()
    {
        var harness = IdentityIntegrationHarness.Create();
        using var scope = harness.CreateScope();
        harness.SetTenant(scope.ServiceProvider);
        var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var mediator = Substitute.For<IMediator>();
        var cache = new FakeNovaCache();

        var handler = new SendForgotPasswordCodeCommandHandler(um, mediator, cache);
        var ctx = HandlerTestHarness.CreateConsumeContext(new SendForgotPasswordCodeCommand { Email = "missing@test.com" });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<SendForgotPasswordCodeResult>(ctx);
        Assert.NotNull(resp);
        Assert.True(resp!.Success);
        // 防枚举：即使用户不存在也不应调用发信
        await mediator.DidNotReceive().Send(Arg.Any<SendEmailCommand>());
    }

    #endregion

    #region RegisterUser

    [Fact]
    public async Task RegisterUser_Success_ReturnsUserId()
    {
        var harness = IdentityIntegrationHarness.Create();
        var cache = new FakeNovaCache();
        await cache.SetAsync("RegisterCode:newbie@test.com", "654321");

        var scopeFactory = harness.Provider.GetRequiredService<IServiceScopeFactory>();
        using var outer = harness.CreateScope();
        harness.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new RegisterUserCommandHandler(scopeFactory, cache, tenantDb);
        var ctx = HandlerTestHarness.CreateConsumeContext(new RegisterUserCommand
        {
            Username = "newbie",
            Email = "newbie@test.com",
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword,
            EmailCode = "654321"
        });

        await handler.Consume(ctx);
        var resp = IdentityIntegrationHarness.GetResponded<RegisterUserResult>(ctx);
        Assert.NotNull(resp);
        Assert.NotEqual(Guid.Empty, resp!.UserId);

        // 种子器（FakeDbInitializer）以"新生成租户"的身份创建了管理员账号，
        // 该账号落在与新租户 TenantId 绑定的表内，需切到新租户上下文才能查到。
        var newTenant = await tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.AdminEmail == "newbie@test.com");
        Assert.NotNull(newTenant);
        using var check = harness.CreateScope();
        harness.SetTenant(check.ServiceProvider, newTenant);
        var um = check.ServiceProvider.GetRequiredService<UserManager<User>>();
        Assert.NotNull(await um.FindByEmailAsync("newbie@test.com"));
    }

    #endregion
}
