using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Commands;
using Nova.Modules.Identity.Domain.Users;
using System.Security.Cryptography;

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

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            await context.RespondAsync(new AdminResetUserPasswordResult { Success = false, Message = "该用户未绑定邮箱，无法通过邮件发送新密码" });
            return;
        }

        string newPassword = GenerateRandomPassword();

        // 使用 Identity 生成密码重置 Token 并重置密码
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            await context.RespondAsync(new AdminResetUserPasswordResult { Success = false, Message = $"密码重置失败: {errors}" });
            return;
        }

        // 更新 SecurityStamp 以防旧 Token 重用
        await _userManager.UpdateSecurityStampAsync(user);

        // 发送包含新密码的邮件给用户
        var emailBody = $@"
            <h3>密码重置通知</h3>
            <p>尊敬的用户 <strong>{user.UserName}</strong>：</p>
            <p>您的账号密码已由管理员重置。重置后的新密码为：<strong>{newPassword}</strong></p>
            <p>为了保障您的账号安全，请在登录后及时修改密码。</p>
        ";

        try
        {
            await _mediator.Send(new SendEmailCommand(
                To: user.Email,
                Subject: "【Nova】账号密码已重置",
                Body: emailBody,
                IsHtml: true
            ));
        }
        catch (Exception ex)
        {
            await context.RespondAsync(new AdminResetUserPasswordResult { Success = false, Message = $"密码重置成功但邮件发送失败: {ex.Message}" });
            return;
        }

        await context.RespondAsync(new AdminResetUserPasswordResult
        {
            Success = true,
            Message = "密码重置成功，新密码已自动发送至用户邮箱"
        });
    }

    private static string GenerateRandomPassword(int length = 10)
    {
        const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowers = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string specials = "!@#$%^&*";

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[length];
        chars[0] = uppers[bytes[0] % uppers.Length];
        chars[1] = lowers[bytes[1] % lowers.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = specials[bytes[3] % specials.Length];

        string allChars = uppers + lowers + digits + specials;
        for (int i = 4; i < length; i++)
        {
            chars[i] = allChars[bytes[i] % allChars.Length];
        }

        for (int i = length - 1; i > 0; i--)
        {
            int j = bytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
