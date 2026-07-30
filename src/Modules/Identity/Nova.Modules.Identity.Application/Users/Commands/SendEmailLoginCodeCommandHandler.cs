using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Caching;
using Nova.Contracts.Commands;
using Nova.Contracts.Exceptions;
using Nova.Framework.Application.Extensions;
using Nova.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Finbuckle.MultiTenant.Abstractions;
using Nova.Modules.Identity.Domain;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class SendEmailLoginCodeCommandHandler : IConsumer<SendEmailLoginCodeCommand>
{
    private readonly NovaTenantDbContext _tenantDb;
    private readonly IMediator _mediator;
    private readonly INovaCache _cache;

    public SendEmailLoginCodeCommandHandler(
        NovaTenantDbContext tenantDb,
        IMediator mediator,
        INovaCache cache)
    {
        _tenantDb = tenantDb;
        _mediator = mediator;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<SendEmailLoginCodeCommand> context)
    {
        var request = context.Message;

        var isEmailValid = await _tenantDb.GlobalUserTenantMappings.AnyAsync(m => m.Account == request.Email);
        if (!isEmailValid)
        {
            // 防止枚举攻击
            await context.RespondAsync(new SendEmailLoginCodeResult { Success = true });
            return;
        }

        var cacheKey = $"RateLimit:SendCode:{request.Email}";
        await _cache.EnsureRateLimitAsync(cacheKey);

        // 使用自定义的 6 位随机验证码放入全局缓存，不绑定特定的租户 User 记录
        var code = Random.Shared.Next(100000, 1000000).ToString();
        await _cache.SetAsync($"LoginCode:{request.Email}", code, TimeSpan.FromMinutes(3));

        Console.WriteLine($"[Nova.Auth] Generated OTP verification code '{code}' for user '{request.Email}'");

        var emailBody = $@"
            <h3>安全登录验证码</h3>
            <p>您的登录验证码是：<strong>{code}</strong></p>
            <p>该验证码在 3 分钟内有效。请勿泄露给他人。</p>
        ";

        // 依靠已重构好的 Notification 模块发送邮件
        await _mediator.Send(new SendEmailCommand(
            To: request.Email,
            Subject: "【Nova】您的登录验证码",
            Body: emailBody,
            IsHtml: true
        ));

        // 设置 60 秒发送冷却时间
        await _cache.SetRateLimitAsync(cacheKey, 60);

        await context.RespondAsync(new SendEmailLoginCodeResult { Success = true });
    }
}
