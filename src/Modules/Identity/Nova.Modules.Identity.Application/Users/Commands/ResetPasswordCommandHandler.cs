using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class ResetPasswordCommandHandler : IConsumer<ResetPasswordCommand>
{
    private readonly UserManager<User> _userManager;

    public ResetPasswordCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task Consume(ConsumeContext<ResetPasswordCommand> context)
    {
        var request = context.Message;
        
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            await context.RespondAsync(new ResetPasswordResult { Success = false, Message = "验证码无效或已过期" });
            return;
        }

        // 验证验证码
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", request.Code);
        if (!isValid)
        {
            await context.RespondAsync(new ResetPasswordResult { Success = false, Message = "验证码无效或已过期" });
            return;
        }

        // 验证码通过后重置密码
        // Identity 的 ResetPasswordAsync 需要一个 PasswordResetToken，由于这里采用的是 6 位数验证码（TOTP），
        // 验证通过后，可以直接生成并消耗一个 PasswordResetToken 或者直接重置密码散列。
        // 为了简便，我们先移除旧密码，再添加新密码（因为用户可能已有密码），或者生成 ResetToken 并重置。
        
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            await context.RespondAsync(new ResetPasswordResult { Success = false, Message = errorMsg });
            return;
        }

        await context.RespondAsync(new ResetPasswordResult { Success = true, Message = "密码重置成功" });
    }
}
