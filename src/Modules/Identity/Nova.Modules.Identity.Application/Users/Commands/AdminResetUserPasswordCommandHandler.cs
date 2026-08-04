using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class AdminResetUserPasswordCommandHandler : IConsumer<AdminResetUserPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IMediator _mediator;

    public AdminResetUserPasswordCommandHandler(UserManager<User> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<AdminResetUserPasswordCommand> context)
    {
        var request = context.Message;

        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
        {
            await context.RespondAsync(new AdminResetUserPasswordResult { Success = false, Message = "用户不存在" });
            return;
        }

        // 被重置的用户必须绑定邮箱，否则无法发送重置邮件
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            await context.RespondAsync(new AdminResetUserPasswordResult { Success = false, Message = "该用户未绑定邮箱，无法发送重置邮件" });
            return;
        }

        // 复用现有「发送忘记密码验证码」逻辑：生成 6 位 TOTP 验证码并发送到用户邮箱，
        // 用户凭验证码在 /reset-password 页面自行设置新密码。
        await _mediator.Send(new SendForgotPasswordCodeCommand { Email = user.Email });

        await context.RespondAsync(new AdminResetUserPasswordResult { Success = true, Message = "重置邮件已发送" });
    }
}
