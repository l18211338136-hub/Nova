using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Caching;
using Nova.Contracts.Commands;
using Nova.Contracts.Exceptions;
using Nova.Framework.Application.Extensions;
using Nova.Framework.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Identity.Domain;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class SendEmailRegisterCodeCommandHandler : IConsumer<SendEmailRegisterCodeCommand>
{
    private readonly IMediator _mediator;
    private readonly INovaCache _cache;
    private readonly NovaTenantDbContext _tenantDbContext;

    public SendEmailRegisterCodeCommandHandler(
        IMediator mediator, 
        INovaCache cache,
        NovaTenantDbContext tenantDbContext)
    {
        _mediator = mediator;
        _cache = cache;
        _tenantDbContext = tenantDbContext;
    }

    public async Task Consume(ConsumeContext<SendEmailRegisterCodeCommand> context)
    {
        var request = context.Message;

        var cacheKey = $"RateLimit:SendCode:{request.Email}";
        await _cache.EnsureRateLimitAsync(cacheKey);
        
        // 校验全局映射表，确保该邮箱在【散户隔离库】中未被注册（允许该邮箱存在于其他 B2B 租户中）
        var targetTenantId = NovaIdentityConstants.Tenants.RetailTenantId;
        var isEmailTaken = await _tenantDbContext.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == request.Email && m.TenantId == targetTenantId);
            
        if (isEmailTaken)
        {
            // 提示用户该邮箱已被注册
            throw new NovaValidationException("该邮箱已被注册，请直接前往登录。");
        }

        // 生成 6 位随机验证码
        var code = Random.Shared.Next(100000, 1000000).ToString();
        
        Console.WriteLine($"[Nova.Auth] Generated OTP verification code '{code}' for registration user '{request.Email}'");

        // 存入缓存，有效期 5 分钟
        await _cache.SetAsync($"RegisterCode:{request.Email}", code, TimeSpan.FromMinutes(5));

        var emailBody = $@"
            <h3>注册验证码</h3>
            <p>您的注册验证码是：<strong>{code}</strong></p>
            <p>该验证码在 5 分钟内有效。请勿泄露给他人。</p>
        ";

        // 依靠已重构好的 Notification 模块发送邮件
        await _mediator.Send(new SendEmailCommand(
            To: request.Email,
            Subject: "【Nova】您的注册验证码",
            Body: emailBody,
            IsHtml: true
        ));

        // 设置 60 秒发送冷却时间
        await _cache.SetRateLimitAsync(cacheKey, 60);

        await context.RespondAsync(new SendEmailRegisterCodeResult { Success = true });
    }
}
