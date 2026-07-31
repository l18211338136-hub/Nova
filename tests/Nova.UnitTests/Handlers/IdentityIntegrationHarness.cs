using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.Extensions;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Contracts.Caching;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Infrastructure;
using NSubstitute;

namespace Nova.UnitTests.Handlers;

#region Fakes

/// <summary>
/// 测试用无操作 IDataProtectionProvider：仅满足 AddDefaultTokenProviders 对 UserManager 的依赖，
/// 测试本身不会真正生成/校验 DataProtector Token（如密码重置、2FA），故用透传实现即可。
/// </summary>
public class NoopDataProtector : IDataProtector
{
    public IDataProtector CreateProtector(string purpose) => this;
    public byte[] Protect(byte[] plaintext) => plaintext;
    public byte[] Unprotect(byte[] protectedData) => protectedData;
}

public class NoopDataProtectionProvider : IDataProtectionProvider
{
    public IDataProtector CreateProtector(string purpose) => new NoopDataProtector();
}

#endregion

#region Fakes

/// <summary>
/// 测试用 ITokenService：生成真实可解析的 JWT（含 NameIdentifier / tenantId claim），
/// 以便 RefreshToken 等 Handler 能用 JwtSecurityTokenHandler 正常读取。不做真实签名校验。
/// </summary>
public class FakeTokenService : ITokenService
{
    public (string Token, int ExpiresIn) GenerateToken(User user, string? tenantId, IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };
        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("tenantId", tenantId));
        }
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("this-is-a-test-secret-key-1234567890abcd"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), 120 * 60);
    }
}

/// <summary>
/// 内存版 INovaCache，供 EmailLogin / Send*Code 等 Handler 使用。
/// </summary>
public class FakeNovaCache : INovaCache
{
    private readonly Dictionary<string, object?> _store = new();
    private readonly object _gate = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        lock (_gate)
        {
            _store.TryGetValue(key, out var v);
            return Task.FromResult((T?)v);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken token = default)
    {
        lock (_gate)
        {
            _store[key] = value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        lock (_gate)
        {
            _store.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken token = default)
    {
        var existing = GetAsync<T>(key, token).GetAwaiter().GetResult();
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }
        var v = factory(token).GetAwaiter().GetResult();
        SetAsync(key, v, expiration, token).GetAwaiter().GetResult();
        return Task.FromResult(v);
    }
}

/// <summary>
/// 测试用 IDbInitializer：仅做种子，不触碰真实数据库。SeedAsync 在调用方已切好租户上下文的
/// scope 内执行，因此会按当前租户创建管理员账号并写入 GlobalUserTenantMappings，
/// 与 RegisterUserCommandHandler 期望的"种子器已建好账号"语义一致。
/// </summary>
public class FakeDbInitializer : IDbInitializer
{
    private readonly IServiceProvider _sp;

    public FakeDbInitializer(IServiceProvider sp) => _sp = sp;

    public Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var accessor = _sp.GetRequiredService<IMultiTenantContextAccessor>();
        var tenant = accessor.MultiTenantContext?.TenantInfo as NovaTenantInfo;
        if (tenant is null)
        {
            return;
        }

        var um = _sp.GetRequiredService<UserManager<User>>();
        var rm = _sp.GetRequiredService<RoleManager<Role>>();

        // 确保 Admin 角色存在（生产种子器会创建），否则 AddToRoleAsync 会失败
        if (!await rm.RoleExistsAsync(NovaIdentityConstants.Roles.Admin))
        {
            await rm.CreateAsync(Role.Create(NovaIdentityConstants.Roles.Admin, "管理员", null, 0));
        }

        var user = User.Create(tenant.AdminEmail!, tenant.AdminEmail!);
        await um.CreateAsync(user, tenant.AdminPassword ?? "Pass@123");
        await um.AddToRoleAsync(user, NovaIdentityConstants.Roles.Admin);

        var tenantDb = _sp.GetRequiredService<NovaTenantDbContext>();
        tenantDb.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
        {
            Account = tenant.AdminEmail!,
            TenantId = tenant.Identifier!
        });
        await tenantDb.SaveChangesAsync();
    }
}

/// <summary>
/// 系统库（NovaTenantDbContext）测试替身：关闭多租户强制，并把 GlobalUserTenantMappings 设为全局表，
/// 避免 InMemory 下 Finbuckle 租户过滤器把跨租户映射表过滤掉，从而让 Login / Register 等可正常读写映射表。
/// </summary>
public class TestTenantDbContext : NovaTenantDbContext
{
    public TestTenantDbContext(DbContextOptions<NovaTenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<GlobalUserTenantMapping>().HasQueryFilter(null);
    }
}

#endregion

/// <summary>
/// B 档集成测试基础设施：用真实 ASP.NET Identity + EF InMemory + Finbuckle + MassTransit Mediator 桩，
/// 搭出一个最小可运行的依赖容器，覆盖 Login / CreateUser / RefreshToken / EmailLogin / Send*Code /
/// Role / RegisterUser 这些重依赖 Handler 的核心路径。
/// </summary>
public class IdentityIntegrationHarness
{
    public IServiceProvider Provider { get; }
    public NovaTenantInfo CurrentTenant { get; }

    private IdentityIntegrationHarness(IServiceProvider provider, NovaTenantInfo tenant)
    {
        Provider = provider;
        CurrentTenant = tenant;
    }

    public static IdentityIntegrationHarness Create(string tenantId = "test-tenant", string? dbSuffix = null)
    {
        dbSuffix ??= Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();

        services.AddHttpContextAccessor();
        services.AddMultiTenant<NovaTenantInfo>().WithInMemoryStore();

        var systemDb = $"system-{tenantId}-{dbSuffix}";
        // 用工厂把系统库解析为 TestTenantDbContext（关闭多租户强制、GlobalUserTenantMappings 设为全局）。
        // 不能直接 AddDbContext<NovaTenantDbContext, TestTenantDbContext>，因为其基类构造函数需要
        // DbContextOptions<NovaTenantDbContext>，而 EF 只会按实现类型注册 DbContextOptions<TestTenantDbContext>。
        services.AddDbContext<NovaTenantDbContext>(options => options.UseInMemoryDatabase(systemDb));
        services.AddScoped<NovaTenantDbContext>(sp =>
            new TestTenantDbContext(sp.GetRequiredService<DbContextOptions<NovaTenantDbContext>>()));
        services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase($"tenant-{tenantId}-{dbSuffix}"));

        services.AddIdentityCore<User>(o =>
            {
                o.Password.RequireUppercase = false;
                // 启用账号锁定（防暴力破解）：测试中用较小阈值以便验证
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, FakeTokenService>();
        services.AddScoped<INovaCache, FakeNovaCache>();
        services.AddScoped<IDbInitializer, FakeDbInitializer>();
        services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider, NoopDataProtectionProvider>();

        // 领域事件分发器（EventBus）：用 NSubstitute 桩记录发布，便于断言审计事件已触发
        var dispatcher = Substitute.For<Nova.Framework.Domain.SeedWork.IDomainEventDispatcher>();
        services.AddSingleton<Nova.Framework.Domain.SeedWork.IDomainEventDispatcher>(dispatcher);

        var config = Substitute.For<IConfiguration>();
        config.GetConnectionString("RetailConnection").Returns("DataSource=:memory:");
        services.AddScoped<IConfiguration>(_ => config);

        // 防御性：确保 ITenantInfo 可从访问器解析（Finbuckle 已注册时本行不生效）
        services.TryAddScoped<ITenantInfo>(sp => sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo!);

        var provider = services.BuildServiceProvider();

        var tenant = new NovaTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = "Test Tenant",
            ConnectionString = "x",
            IsActive = true,
            AdminEmail = "admin@test.com",
            AdminPassword = "Pass@123"
        };

        return new IdentityIntegrationHarness(provider, tenant);
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    /// <summary>在给定 scope 的 ServiceProvider 上设置当前租户上下文（供 UserManager / RoleManager 使用）。</summary>
    public void SetTenant(IServiceProvider sp, NovaTenantInfo? tenant = null)
    {
        tenant ??= CurrentTenant;
        var setter = sp.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenant);
    }

    /// <summary>读取 ConsumeContext 上 RespondAsync 回包的强类型结果（取最后一次调用）。</summary>
    public static T? GetResponded<T>(ConsumeContext context) where T : class
    {
        T? result = null;
        foreach (var call in context.ReceivedCalls())
        {
            if (call.GetMethodInfo().Name == "RespondAsync" && call.GetArguments().Length > 0)
            {
                result = call.GetArguments()[0] as T;
            }
        }
        return result;
    }
}
