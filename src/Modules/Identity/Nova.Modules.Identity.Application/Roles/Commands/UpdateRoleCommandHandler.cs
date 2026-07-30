using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Roles;

namespace Nova.Modules.Identity.Application.Roles.Commands;

public class UpdateRoleCommandHandler : IConsumer<UpdateRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;

    public UpdateRoleCommandHandler(RoleManager<Role> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task Consume(ConsumeContext<UpdateRoleCommand> context)
    {
        var command = context.Message;

        var role = await _roleManager.FindByIdAsync(command.Id.ToString());
        if (role == null)
        {
            throw new NovaValidationException($"找不到 ID 为 '{command.Id}' 的角色");
        }

        var existingRole = await _roleManager.FindByNameAsync(command.Name);
        if (existingRole != null && existingRole.Id != role.Id)
        {
            throw new NovaValidationException($"角色名称 '{command.Name}' 已被其他角色使用");
        }

        role.Update(
            command.Name,
            command.DisplayName,
            command.Remarks,
            command.Sort,
            command.IsEnabled
        );

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"更新角色失败: {errors}");
        }

        if (command.Permissions != null)
        {
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();
            
            // Remove obsolete permissions
            foreach (var claim in permissionClaims)
            {
                if (!command.Permissions.Contains(claim.Value))
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }
            }

            // Add new permissions
            var existingVals = permissionClaims.Select(c => c.Value).ToHashSet();
            foreach (var perm in command.Permissions)
            {
                if (!existingVals.Contains(perm))
                {
                    await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", perm));
                }
            }
        }

        if (command.Menus != null)
        {
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var menuClaims = existingClaims.Where(c => c.Type == "Menu").ToList();
            
            // Remove obsolete menus
            foreach (var claim in menuClaims)
            {
                if (!command.Menus.Contains(claim.Value))
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }
            }

            // Add new menus
            var existingMenuVals = menuClaims.Select(c => c.Value).ToHashSet();
            foreach (var menu in command.Menus)
            {
                if (!existingMenuVals.Contains(menu))
                {
                    await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Menu", menu));
                }
            }
        }

        await context.RespondAsync(new UpdateRoleResult { Success = true });
    }
}
