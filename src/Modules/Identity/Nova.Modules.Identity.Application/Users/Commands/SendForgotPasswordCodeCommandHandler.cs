using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Caching;
using Nova.Contracts.Commands;
using Nova.Contracts.Exceptions;
using Nova.Framework.Application.Extensions;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class SendForgotPasswordCodeCommandHandler : IConsumer<SendForgotPasswordCodeCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IMediator _mediator;
    private readonly INovaCache _cache;

    public SendForgotPasswordCodeCommandHandler(UserManager<User> userManager, IMediator mediator, INovaCache cache)
    {
        _userManager = userManager;
        _mediator = mediator;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<SendForgotPasswordCodeCommand> context)
    {
        var request = context.Message;
        
        var cacheKey = $"RateLimit:SendCode:{request.Email}";
        await _cache.EnsureRateLimitAsync(cacheKey);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // 为防止用户枚举攻击，即使用户不存在也不应该返回报错
            await context.RespondAsync(new SendForgotPasswordCodeResult { Success = true });
            return;
        }

        // 使用 Identity 自带的 DefaultEmailProvider (基于 TOTP 算法) 生成 6 位数验证码
        var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        var emailBody = $@"
            <h3>重置密码验证码</h3>
            <p>您正在进行重置密码操作，您的验证码是：<strong>{code}</strong></p>
            <p>该验证码在 3 分钟内有效。如果这不是您的操作，请忽略此邮件。</p>
        ";

        await _mediator.Send(new SendEmailCommand(
            To: request.Email,
            Subject: "【Nova】重置密码验证码",
            Body: emailBody,
            IsHtml: true
        ));

        // 设置 60 秒发送冷却时间
        await _cache.SetRateLimitAsync(cacheKey, 60);

        await context.RespondAsync(new SendForgotPasswordCodeResult { Success = true });
    }
}
