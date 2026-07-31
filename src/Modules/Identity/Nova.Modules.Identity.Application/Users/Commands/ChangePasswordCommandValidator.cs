using FluentValidation;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("旧密码不能为空");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .MinimumLength(6).WithMessage("新密码长度至少为6位")
            .NotEqual(x => x.OldPassword).WithMessage("新密码不能与旧密码相同");
    }
}
