using FluentValidation;

namespace Nova.Modules.Identity.Application.Roles.Commands;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("角色名称不能为空");
        RuleFor(x => x.Permissions)
            .Must(perms => perms == null || !perms.Contains("*"))
            .WithMessage("权限点格式不正确或包含不支持的特殊符号");
    }
}
