using FluentValidation;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("用户 ID 不能为空");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("邮箱格式不正确");
        RuleFor(x => x.Permissions)
            .Must(perms => perms == null || !perms.Contains("*"))
            .WithMessage("权限点格式不正确或包含不支持的特殊符号");
    }
}
