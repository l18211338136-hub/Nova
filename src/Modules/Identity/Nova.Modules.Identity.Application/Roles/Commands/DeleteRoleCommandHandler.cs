using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Roles;

namespace Nova.Modules.Identity.Application.Roles.Commands;

public class DeleteRoleCommandHandler : IConsumer<DeleteRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;

    public DeleteRoleCommandHandler(RoleManager<Role> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task Consume(ConsumeContext<DeleteRoleCommand> context)
    {
        var command = context.Message;

        var role = await _roleManager.FindByIdAsync(command.Id.ToString());
        if (role == null)
        {
            throw new NovaValidationException($"找不到 ID 为 '{command.Id}' 的角色");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"删除角色失败: {errors}");
        }

        await context.RespondAsync(new DeleteRoleResult { Success = true });
    }
}
