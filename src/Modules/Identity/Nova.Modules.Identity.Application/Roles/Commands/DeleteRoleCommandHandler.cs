using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Modules.Identity.Application.Events;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Roles.Commands;

public class DeleteRoleCommandHandler : IConsumer<DeleteRoleCommand>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IDomainEventDispatcher _dispatcher;

    public DeleteRoleCommandHandler(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IDomainEventDispatcher dispatcher)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dispatcher = dispatcher;
    }

    public async Task Consume(ConsumeContext<DeleteRoleCommand> context)
    {
        var command = context.Message;

        var role = await _roleManager.FindByIdAsync(command.Id.ToString());
        if (role == null)
        {
            throw new NovaValidationException($"找不到 ID 为 '{command.Id}' 的角色");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"删除角色失败: {errors}");
        }

        foreach (var u in usersInRole)
        {
            await _dispatcher.PublishAsync(new UserPermissionsUpdatedEvent(u.Id));
        }

        await context.RespondAsync(new DeleteRoleResult { Success = true });
    }
}
