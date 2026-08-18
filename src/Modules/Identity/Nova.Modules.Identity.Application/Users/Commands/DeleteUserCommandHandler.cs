using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Events;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class DeleteUserCommandHandler : IConsumer<DeleteUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly NovaTenantDbContext? _tenantDb;

    public DeleteUserCommandHandler(
        UserManager<User> userManager,
        IDomainEventDispatcher dispatcher,
        NovaTenantDbContext? tenantDb = null)
    {
        _userManager = userManager;
        _dispatcher = dispatcher;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<DeleteUserCommand> context)
    {
        var command = context.Message;

        var user = await _userManager.FindByIdAsync(command.Id.ToString());
        if (user == null) throw new NovaValidationException("用户不存在");

        var userId = user.Id;
        var oldEmail = user.Email;
        var oldUserName = user.UserName;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户删除失败: {errors}");
        }

        // BUG-05: 清理 GlobalUserTenantMappings 孤儿记录
        if (_tenantDb != null)
        {
            var mappings = await _tenantDb.GlobalUserTenantMappings
                .Where(m => m.Account == oldEmail || m.Account == oldUserName)
                .ToListAsync();
            if (mappings.Any())
            {
                _tenantDb.GlobalUserTenantMappings.RemoveRange(mappings);
                await _tenantDb.SaveChangesAsync();
            }
        }

        await _dispatcher.PublishAsync(new UserPermissionsUpdatedEvent(userId));

        await context.RespondAsync(new DeleteUserResult { Success = true });
    }
}
