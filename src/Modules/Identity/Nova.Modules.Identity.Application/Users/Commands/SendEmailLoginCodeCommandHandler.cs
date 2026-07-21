using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Commands;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class SendEmailLoginCodeCommandHandler : IConsumer<SendEmailLoginCodeCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IMediator _mediator;

    public SendEmailLoginCodeCommandHandler(UserManager<User> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<SendEmailLoginCodeCommand> context)
    {
        var request = context.Message;
        
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // 为防止用户枚举攻击，即使用户不存在也不应该返回报错
            // 可以直接抛出一个通用异常或者返回成功。此处选择直接返回
            await context.RespondAsync(new SendEmailLoginCodeResult { Success = true });
            return;
        }

        // 使用 Identity 自带的 DefaultEmailProvider (基于 TOTP 算法) 生成 6 位数验证码
        var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

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

        await context.RespondAsync(new SendEmailLoginCodeResult { Success = true });
    }
}
