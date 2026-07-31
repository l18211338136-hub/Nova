using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Menus;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.UnitTests.Handlers;

/// <summary>
/// 轻量内存版 IIdentityDbContext，仅建模 Menu，规避 Finbuckle 多租户强制与全局软删除过滤器
/// 对 EF InMemory 的干扰，同时保留接口契约供 Handler 使用。
/// </summary>
public class TestIdentityDbContext : DbContext, IIdentityDbContext
{
    public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<Menu> Menus { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User/Role 由 ASP.NET Identity 管理，A 档 Menu 测试不需要，忽略以免引入复杂导航建模
        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<Role>();

        modelBuilder.Entity<Menu>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).IsRequired();
            b.Property(m => m.Path).IsRequired();
            b.Property(m => m.Component).IsRequired();
        });
    }
}

public static class HandlerTestHarness
{
    public static TestIdentityDbContext CreateInMemoryIdentityDb(string dbName = "")
    {
        if (string.IsNullOrEmpty(dbName))
        {
            dbName = System.Guid.NewGuid().ToString();
        }

        var options = new DbContextOptionsBuilder<TestIdentityDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestIdentityDbContext(options);
    }

    /// <summary>
    /// 用 NSubstitute 构造一个最小的 ConsumeContext，只正确提供 Message / CancellationToken，
    /// RespondAsync 默认返回已完成的 Task（未配置时 NSubstitute 返回 default，对 Task 即已完成）。
    /// </summary>
    public static ConsumeContext<T> CreateConsumeContext<T>(T message) where T : class
    {
        var ctx = Substitute.For<ConsumeContext<T>>();
        ctx.Message.Returns(message);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }
}

/// <summary>
/// 最小 IBackgroundJobClient 桩：真实实现 Create/ChangeState，收集被 Enqueue 的 Job 供断言。
/// 用于绕开 NSubstitute 无法拦截 Hangfire 扩展方法 Enqueue 导致的 Job.FromExpression 崩溃。
/// </summary>
public class FakeBackgroundJobClient : IBackgroundJobClient
{
    public System.Collections.Generic.List<Job> EnqueuedJobs { get; } = new();

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job);
        return System.Guid.NewGuid().ToString("N");
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;

    public void Dispose() { }
}
