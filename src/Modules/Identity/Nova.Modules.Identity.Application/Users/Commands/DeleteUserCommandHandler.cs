using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Modules.Identity.Application.Events;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class DeleteUserCommandHandler : IConsumer<DeleteUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IDomainEventDispatcher _dispatcher;

    public DeleteUserCommandHandler(UserManager<User> userManager, IDomainEventDispatcher dispatcher)
    {
        _userManager = userManager;
        _dispatcher = dispatcher;
    }

    public async Task Consume(ConsumeContext<DeleteUserCommand> context)
    {
        var command = context.Message;

        var user = await _userManager.FindByIdAsync(command.Id.ToString());
        if (user == null) throw new NovaValidationException("用户不存在");

        var userId = user.Id;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户删除失败: {errors}");
        }

        await _dispatcher.PublishAsync(new UserPermissionsUpdatedEvent(userId));

        await context.RespondAsync(new DeleteUserResult { Success = true });
    }
}
