using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 系统库（NovaTenantDbContext）测试替身。
/// <para>
/// 覆写 <see cref="OnModelCreating"/> 以：
/// <list type="bullet">
///   <item>移除 <see cref="GlobalUserTenantMapping"/> 上的多租户查询过滤器，
///         使映射表对跨租户读写可见（EF InMemory 不支持 Finbuckle 的租户过滤器，
///         直接查询会因过滤器返回空集而导致 Login / Register 流程失败）。</item>
/// </list>
/// </para>
/// <remarks>
/// 不能通过 <c>AddDbContext&lt;NovaTenantDbContext, TestTenantDbContext&gt;</c> 注册，
/// 因为基类构造函数需要 <c>DbContextOptions&lt;NovaTenantDbContext&gt;</c>，
/// EF 只按实现类型注册 Options，需用工厂方式手动解析。
/// </remarks>
/// </summary>
public sealed class TestTenantDbContext : NovaTenantDbContext
{
    public TestTenantDbContext(DbContextOptions<NovaTenantDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 移除多租户强制过滤器，让全局账号映射表在测试中可跨租户读写
        modelBuilder.Entity<GlobalUserTenantMapping>().HasQueryFilter(null);
    }
}
