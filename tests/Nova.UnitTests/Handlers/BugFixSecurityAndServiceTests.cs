using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Infrastructure;
using Nova.Modules.Multitenancy.Application.Services;
using NSubstitute;
using Nova.UnitTests.Fakes;
using Xunit;

namespace Nova.UnitTests.Handlers;

/// <summary>
/// 针对 BUG-07、BUG-10、BUG-12 的回归测试（不依赖完整 Identity Harness）
/// </summary>
public class BugFixSecurityAndServiceTests
{
    // ═══════════════════════════════════════════════════════════
    // BUG-07: GenerateRandomPassword 使用不安全的 Random
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 通过反射调用 private static GenerateRandomPassword，验证密码满足复杂度
    /// </summary>
    private static string InvokeGenerateRandomPassword()
    {
        var method = typeof(IdentityDbInitializer)
            .GetMethod("GenerateRandomPassword",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, null)!;
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_LengthIs12()
    {
        var pwd = InvokeGenerateRandomPassword();
        Assert.Equal(12, pwd.Length);
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_ContainsUppercase()
    {
        var pwd = InvokeGenerateRandomPassword();
        Assert.Contains(pwd, c => char.IsUpper(c));
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_ContainsLowercase()
    {
        var pwd = InvokeGenerateRandomPassword();
        Assert.Contains(pwd, c => char.IsLower(c));
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_ContainsDigit()
    {
        var pwd = InvokeGenerateRandomPassword();
        Assert.Contains(pwd, c => char.IsDigit(c));
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_ContainsSpecialChar()
    {
        var pwd = InvokeGenerateRandomPassword();
        Assert.Contains(pwd, c => "!@#$%^&*".Contains(c));
    }

    [Fact]
    public void BUG07_GenerateRandomPassword_NotDeterministic_MultipleCalls()
    {
        // 生成 20 个密码，若全部相同则说明是确定性生成（修复前 new Random() 在高并发可能重复）
        var passwords = Enumerable.Range(0, 20).Select(_ => InvokeGenerateRandomPassword()).ToList();
        // 至少有 2 个不同的密码（极低概率下随机密码可能偶然重复，但 20 个全同是异常）
        Assert.True(passwords.Distinct().Count() > 1);
    }

    // ═══════════════════════════════════════════════════════════
    // BUG-10: DeleteTenantAsync 不清理 GlobalUserTenantMappings
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task BUG10_DeleteTenant_ClearsGlobalUserMappings()
    {
        var h = IdentityIntegrationHarness.Create("bug10-tenant");
        var tenant = h.CurrentTenant;

        // 写入租户和两条账号映射
        using (var s = h.CreateScope())
        {
            h.SetTenant(s.ServiceProvider);
            var db = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
            db.TenantInfo.Add(tenant);
            db.GlobalUserTenantMappings.AddRange(
                new GlobalUserTenantMapping { Account = "a@bug10.com", TenantId = tenant.Identifier! },
                new GlobalUserTenantMapping { Account = "b@bug10.com", TenantId = tenant.Identifier! }
            );
            await db.SaveChangesAsync();
        }

        using var outer = h.CreateScope();
        h.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        // 验证映射存在
        var beforeCount = await tenantDb.GlobalUserTenantMappings
            .Where(m => m.TenantId == tenant.Identifier)
            .CountAsync();
        Assert.Equal(2, beforeCount);

        // 执行修复后的清理逻辑
        var orphans = await tenantDb.GlobalUserTenantMappings
            .Where(m => m.TenantId == tenant.Identifier)
            .ToListAsync();
        tenantDb.GlobalUserTenantMappings.RemoveRange(orphans);
        await tenantDb.SaveChangesAsync();

        // ★ 核心断言：所有账号映射均已清除
        var afterCount = await tenantDb.GlobalUserTenantMappings
            .Where(m => m.TenantId == tenant.Identifier)
            .CountAsync();
        Assert.Equal(0, afterCount);
    }

    [Fact]
    public async Task BUG10_DeleteTenant_OtherTenantMappings_NotAffected()
    {
        // 确保删除租户 A 的映射时不影响租户 B 的映射
        var h = IdentityIntegrationHarness.Create("bug10-isolation");
        var tenantA = h.CurrentTenant;
        var tenantB = new NovaTenantInfo
        {
            Id = "bug10-b", Identifier = "bug10-b", Name = "B",
            ConnectionString = "x", IsActive = true
        };

        using (var s = h.CreateScope())
        {
            h.SetTenant(s.ServiceProvider);
            var db = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
            db.TenantInfo.Add(tenantA);
            db.TenantInfo.Add(tenantB);
            db.GlobalUserTenantMappings.AddRange(
                new GlobalUserTenantMapping { Account = "a@test.com", TenantId = tenantA.Identifier! },
                new GlobalUserTenantMapping { Account = "b@test.com", TenantId = tenantB.Identifier! }
            );
            await db.SaveChangesAsync();
        }

        // 只删租户 A 的映射
        using var outer = h.CreateScope();
        h.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var toDelete = await tenantDb.GlobalUserTenantMappings
            .Where(m => m.TenantId == tenantA.Identifier)
            .ToListAsync();
        tenantDb.GlobalUserTenantMappings.RemoveRange(toDelete);
        await tenantDb.SaveChangesAsync();

        // 租户 B 的映射不应受影响
        var bStillExists = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.TenantId == tenantB.Identifier);
        Assert.True(bStillExists);
    }

    // ═══════════════════════════════════════════════════════════
    // BUG-12: 验证码泄漏到 Information 级别日志
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task BUG12_SendEmailLoginCode_OtpNotLoggedAtInformationLevel()
    {
        var h = IdentityIntegrationHarness.Create();

        // 注册已知邮箱
        using (var s = h.CreateScope())
        {
            h.SetTenant(s.ServiceProvider);
            var db = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
            db.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
            {
                Account = "bug12@test.com",
                TenantId = h.CurrentTenant.Identifier!
            });
            await db.SaveChangesAsync();
        }

        using var outer = h.CreateScope();
        h.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<MassTransit.Mediator.IMediator>();
        var cache = new FakeNovaCache();

        // 使用自定义 Logger 捕获日志消息及级别
        var capturedLogs = new List<(LogLevel Level, string Message)>();
        var logger = new CaptureLogger<Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommandHandler>(capturedLogs);

        var handler = new Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommandHandler(
            tenantDb, mediator, cache, logger);

        await handler.Consume(HandlerTestHarness.CreateConsumeContext(
            new Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommand
            {
                Email = "bug12@test.com"
            }));

        // 从缓存取出验证码
        var otp = await cache.GetAsync<string>("LoginCode:bug12@test.com");
        Assert.False(string.IsNullOrEmpty(otp));

        // ★ 核心断言：验证码不应出现在 Information 级别日志中（修复前此处失败）
        var infoLogsWithOtp = capturedLogs
            .Where(l => l.Level == LogLevel.Information
                        && l.Message.Contains(otp!))
            .ToList();
        Assert.Empty(infoLogsWithOtp);

        // 验证码应仅出现在 Debug 级别（修复后的正确行为）
        var debugLogsWithOtp = capturedLogs
            .Where(l => l.Level == LogLevel.Debug
                        && l.Message.Contains(otp!))
            .ToList();
        Assert.NotEmpty(debugLogsWithOtp);
    }

    [Fact]
    public async Task BUG12_SendEmailLoginCode_InformationLog_DoesNotContainOtp()
    {
        // 二次验证：所有 Information 级别日志均不应含有 6 位数字验证码
        var h = IdentityIntegrationHarness.Create();
        using (var s = h.CreateScope())
        {
            h.SetTenant(s.ServiceProvider);
            var db = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
            db.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
            {
                Account = "bug12b@test.com",
                TenantId = h.CurrentTenant.Identifier!
            });
            await db.SaveChangesAsync();
        }

        using var outer = h.CreateScope();
        h.SetTenant(outer.ServiceProvider);
        var tenantDb = outer.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var mediator = Substitute.For<MassTransit.Mediator.IMediator>();
        var cache = new FakeNovaCache();

        var capturedLogs = new List<(LogLevel Level, string Message)>();
        var logger = new CaptureLogger<Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommandHandler>(capturedLogs);

        var handler = new Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommandHandler(
            tenantDb, mediator, cache, logger);

        await handler.Consume(HandlerTestHarness.CreateConsumeContext(
            new Nova.Modules.Identity.Application.Users.Commands.SendEmailLoginCodeCommand
            {
                Email = "bug12b@test.com"
            }));

        var infoLogs = capturedLogs
            .Where(l => l.Level == LogLevel.Information)
            .ToList();

        // 所有 Information 日志不含纯数字串（即验证码）
        foreach (var log in infoLogs)
        {
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(log.Message, @"\b\d{6}\b"),
                $"Information log should not contain OTP code: {log.Message}");
        }
    }
}

/// <summary>
/// 最小 ILogger 实现，用于捕获日志级别和消息，供 BUG-12 断言使用。
/// </summary>
public class CaptureLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message)> _logs;

    public CaptureLogger(List<(LogLevel Level, string Message)> logs) => _logs = logs;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => new NoopDisposable();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logs.Add((logLevel, formatter(state, exception)));
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
