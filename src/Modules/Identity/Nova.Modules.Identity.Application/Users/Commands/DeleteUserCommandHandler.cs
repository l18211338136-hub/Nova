using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class DeleteUserCommandHandler : IConsumer<DeleteUserCommand>
{
    private readonly UserManager<User> _userManager;

    public DeleteUserCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task Consume(ConsumeContext<DeleteUserCommand> context)
    {
        var command = context.Message;

        var user = await _userManager.FindByIdAsync(command.Id.ToString());
        if (user == null) throw new NovaValidationException("用户不存在");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户删除失败: {errors}");
        }

        await context.RespondAsync(new DeleteUserResult { Success = true });
    }
}
