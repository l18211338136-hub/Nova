using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Caching;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Infrastructure;
using Nova.UnitTests.Fakes;
using NSubstitute;

namespace Nova.UnitTests.Handlers;

/// <summary>
/// Identity 模块集成测试基础设施。
/// <para>
/// 用真实 ASP.NET Identity + EF InMemory + Finbuckle + MassTransit Mediator 桩，
/// 搭出一个最小可运行的依赖容器，覆盖 Login / CreateUser / RefreshToken /
/// EmailLogin / Send*Code / Role / RegisterUser 等重依赖 Handler 的核心路径。
/// </para>
/// <para>
/// 设计原则：Harness 只负责"容器组装"和"操作辅助"两件事。
/// 所有测试替身（Fakes）均独立放在 <c>Nova.UnitTests.Fakes</c> 命名空间下。
/// </para>
/// </summary>
public class IdentityIntegrationHarness
{
    // ── 公开属性 ────────────────────────────────────────────────

    public IServiceProvider Provider { get; }
    public NovaTenantInfo CurrentTenant { get; }

    // ── 构造 ────────────────────────────────────────────────────

    private IdentityIntegrationHarness(IServiceProvider provider, NovaTenantInfo tenant)
    {
        Provider = provider;
        CurrentTenant = tenant;
    }

    // ── 工厂 ────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个独立隔离的 Harness 实例。
    /// 每次调用都生成新的 InMemory 数据库（通过 <paramref name="dbSuffix"/> 保证唯一性），
    /// 使测试之间不共享状态。
    /// </summary>
    /// <param name="tenantId">初始租户 ID，默认 "test-tenant"。</param>
    /// <param name="dbSuffix">数据库名后缀，默认随机生成 GUID。</param>
    public static IdentityIntegrationHarness Create(
        string tenantId = "test-tenant", string? dbSuffix = null)
    {
        dbSuffix ??= Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();

        // ── 多租户 ──────────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddMultiTenant<NovaTenantInfo>().WithInMemoryStore();

        // ── 数据库 ──────────────────────────────────────────────
        // 系统库：用工厂注册 TestTenantDbContext（移除了多租户查询过滤器），
        // 不能直接 AddDbContext<NovaTenantDbContext, TestTenantDbContext>，
        // 原因详见 TestTenantDbContext 的 XML 注释。
        var systemDbName = $"system-{tenantId}-{dbSuffix}";
        services.AddDbContext<NovaTenantDbContext>(o => o.UseInMemoryDatabase(systemDbName));
        services.AddScoped<NovaTenantDbContext>(sp =>
            new TestTenantDbContext(
                sp.GetRequiredService<DbContextOptions<NovaTenantDbContext>>()));

        // 业务库（Identity）：每个租户独立数据库
        services.AddDbContext<IdentityDbContext>(o =>
            o.UseInMemoryDatabase($"tenant-{tenantId}-{dbSuffix}"));

        // ── ASP.NET Identity ────────────────────────────────────
        services.AddIdentityCore<Nova.Modules.Identity.Domain.Users.User>(o =>
        {
            o.Password.RequireUppercase = false;
            o.Lockout.AllowedForNewUsers = true;
            o.Lockout.MaxFailedAccessAttempts = 3; // 测试中使用较小阈值便于验证锁定逻辑
        })
            .AddRoles<Nova.Modules.Identity.Domain.Roles.Role>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        // ── 测试替身（Fakes）───────────────────────────────────
        services.AddScoped<ITokenService, FakeTokenService>();
        services.AddScoped<INovaCache, FakeNovaCache>();
        services.AddScoped<IDbInitializer, FakeDbInitializer>();
        services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider,
            NoopDataProtectionProvider>();

        // ── 领域事件（用 NSubstitute 桩，便于断言审计事件）────
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        services.AddSingleton<IDomainEventDispatcher>(dispatcher);

        // ── 其他依赖 ────────────────────────────────────────────
        var config = Substitute.For<IConfiguration>();
        config.GetConnectionString("RetailConnection").Returns("DataSource=:memory:");
        var jwtSection = Substitute.For<IConfigurationSection>();
        jwtSection["SecretKey"].Returns(FakeTokenService.TestSecretKey);
        config.GetSection("Jwt").Returns(jwtSection);
        services.AddScoped<IConfiguration>(_ => config);

        // 确保 ITenantInfo 可从访问器解析（Finbuckle 已注册时本行不生效）
        services.TryAddScoped<ITenantInfo>(sp =>
            sp.GetRequiredService<IMultiTenantContextAccessor>()
              .MultiTenantContext?.TenantInfo!);

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

    // ── 操作辅助 ────────────────────────────────────────────────

    /// <summary>从根容器创建新的依赖注入 Scope。</summary>
    public IServiceScope CreateScope() => Provider.CreateScope();

    /// <summary>
    /// 在给定 <paramref name="sp"/> 上设置当前租户上下文，
    /// 供 UserManager / RoleManager 等多租户感知服务使用。
    /// </summary>
    /// <param name="sp">要设置租户的 ServiceProvider（通常是某个 Scope 内的）。</param>
    /// <param name="tenant">目标租户，默认为 <see cref="CurrentTenant"/>。</param>
    public void SetTenant(IServiceProvider sp, NovaTenantInfo? tenant = null)
    {
        tenant ??= CurrentTenant;
        var setter = sp.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenant);
    }

    /// <summary>
    /// 从 NSubstitute 拦截到的 <see cref="ConsumeContext"/> 调用记录中，
    /// 提取最后一次 <c>RespondAsync</c> 调用的强类型回包。
    /// </summary>
    /// <typeparam name="T">期望的回包类型。</typeparam>
    /// <param name="context">经过 Handler 消费的 ConsumeContext Mock。</param>
    /// <returns>最后一次 RespondAsync 的参数；若未调用则返回 null。</returns>
    public static T? GetResponded<T>(ConsumeContext context) where T : class
    {
        T? result = null;
        foreach (var call in context.ReceivedCalls())
        {
            if (call.GetMethodInfo().Name == "RespondAsync"
                && call.GetArguments().Length > 0)
            {
                result = call.GetArguments()[0] as T;
            }
        }
        return result;
    }
}
