using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Roles;

namespace Nova.Modules.Identity.Application.Roles.Commands;

public class CreateRoleCommandHandler : IConsumer<CreateRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;

    public CreateRoleCommandHandler(RoleManager<Role> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task Consume(ConsumeContext<CreateRoleCommand> context)
    {
        var command = context.Message;

        var existingRole = await _roleManager.FindByNameAsync(command.Name);
        if (existingRole != null)
        {
            throw new NovaValidationException($"角色名称 '{command.Name}' 已存在");
        }

        var role = Role.Create(
            command.Name,
            command.DisplayName,
            command.Remarks,
            command.Sort,
            command.IsEnabled
        );

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"创建角色失败: {errors}");
        }

        if (command.Permissions != null && command.Permissions.Any())
        {
            foreach (var perm in command.Permissions)
            {
                await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", perm));
            }
        }

        await context.RespondAsync(new CreateRoleResult { RoleId = role.Id });
    }
}
