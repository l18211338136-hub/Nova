using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Nova.Contracts.Commands;
using Nova.Contracts.Exceptions;
using Nova.Framework.Application.Extensions;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class SendEmailRegisterCodeCommandHandler : IConsumer<SendEmailRegisterCodeCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;

    public SendEmailRegisterCodeCommandHandler(UserManager<User> userManager, IMediator mediator, IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _mediator = mediator;
        _memoryCache = memoryCache;
    }

    public async Task Consume(ConsumeContext<SendEmailRegisterCodeCommand> context)
    {
        var request = context.Message;

        var cacheKey = $"RateLimit:SendCode:{request.Email}";
        _memoryCache.EnsureRateLimit(cacheKey);
        
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            // 提示用户该邮箱已被注册
            throw new NovaValidationException("该邮箱已被注册，请直接前往登录。");
        }

        // 生成 6 位随机验证码
        var code = Random.Shared.Next(100000, 1000000).ToString();

        // 存入缓存，有效期 5 分钟
        _memoryCache.Set($"RegisterCode:{request.Email}", code, TimeSpan.FromMinutes(5));

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
        _memoryCache.SetRateLimit(cacheKey, 60);

        await context.RespondAsync(new SendEmailRegisterCodeResult { Success = true });
    }
}
