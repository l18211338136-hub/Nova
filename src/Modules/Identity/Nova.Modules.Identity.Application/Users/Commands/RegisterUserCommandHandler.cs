using Nova.Contracts.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class RegisterUserCommandHandler : IConsumer<RegisterUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _memoryCache;

    public RegisterUserCommandHandler(UserManager<User> userManager, IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _memoryCache = memoryCache;
    }

    public async Task Consume(ConsumeContext<RegisterUserCommand> context)
    {
        var command = context.Message;
        
        if (!_memoryCache.TryGetValue("RegisterCode:$($command.Email)", out string? cachedCode) || cachedCode != command.EmailCode)
        {
            throw new NovaValidationException("验证码错误或已过期");
        }

        // 验证成功后清理缓存
        _memoryCache.Remove("RegisterCode:$($command.Email)");

        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            throw new NovaValidationException("该邮箱已经被注册");
        }

        var existingUsername = await _userManager.FindByNameAsync(command.Username);
        if (existingUsername != null)
        {
            throw new NovaValidationException("该用户名已经被使用");
        }

        var user = new User
        {
            UserName = command.Username,
            Email = command.Email,
            EmailConfirmed = true,
            IsEnabled = true
        };

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户注册失败: {errors}");
        }

        // 可以选择分配默认角色，例如：
        // await _userManager.AddToRoleAsync(user, NovaIdentityConstants.Roles.User);

        await context.RespondAsync(new RegisterUserResult { UserId = user.Id });
    }
}
