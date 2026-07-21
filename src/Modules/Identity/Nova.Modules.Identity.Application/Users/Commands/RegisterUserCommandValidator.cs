using FluentValidation;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .MinimumLength(3).WithMessage("用户名长度至少为3位");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度至少为6位");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("两次输入的密码不一致");

        RuleFor(x => x.EmailCode)
            .NotEmpty().WithMessage("验证码不能为空")
            .Length(6).WithMessage("验证码长度必须为6位");
    }
}
