using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Users;
using Nova.Framework.MultiTenancy;
using Finbuckle.MultiTenant.Abstractions;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class CreateUserCommandHandler : IConsumer<CreateUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly NovaTenantDbContext _tenantDbContext;
    private readonly ITenantInfo? _tenantInfo;

    public CreateUserCommandHandler(
        UserManager<User> userManager,
        NovaTenantDbContext tenantDbContext,
        ITenantInfo? tenantInfo = null)
    {
        _userManager = userManager;
        _tenantDbContext = tenantDbContext;
        _tenantInfo = tenantInfo;
    }

    public async Task Consume(ConsumeContext<CreateUserCommand> context)
    {
        var command = context.Message;

        var existingEmail = await _userManager.FindByEmailAsync(command.Email);
        if (existingEmail != null) throw new NovaValidationException("该邮箱已经被使用");

        var existingUsername = await _userManager.FindByNameAsync(command.UserName);
        if (existingUsername != null) throw new NovaValidationException("该用户名已经被使用");

        var user = User.Create(command.UserName, command.Email);

        if (!command.IsEnabled)
        {
            user.Disable();
        }

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户创建失败: {errors}");
        }

        // 必须在 CreateAsync 之后设置手机号：此时 user 才被 EF 跟踪，
        // 否则 Finbuckle EnforceMultiTenant() 会在 SaveChanges 时因 TenantId 未设置而抛异常。
        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, command.PhoneNumber);
            if (!phoneResult.Succeeded)
            {
                var errors = string.Join(", ", phoneResult.Errors.Select(e => e.Description));
                throw new NovaValidationException($"用户手机号设置失败: {errors}");
            }
        }

        if (command.Roles != null && command.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, command.Roles);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new NovaValidationException($"用户角色分配失败: {errors}");
            }
        }

        if (command.Permissions != null && command.Permissions.Any())
        {
            var claims = command.Permissions.Select(p => new System.Security.Claims.Claim("Permission", p)).ToList();
            var claimsResult = await _userManager.AddClaimsAsync(user, claims);
            if (!claimsResult.Succeeded)
            {
                var errors = string.Join(", ", claimsResult.Errors.Select(e => e.Description));
                throw new NovaValidationException($"用户直接权限分配失败: {errors}");
            }
        }

        if (_tenantInfo != null)
        {
            _tenantDbContext.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
            {
                Account = user.Email,
                TenantId = _tenantInfo.Identifier!
            });
            await _tenantDbContext.SaveChangesAsync();
        }

        await context.RespondAsync(new CreateUserResult { UserId = user.Id });
    }
}
