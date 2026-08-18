using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Events;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class UpdateUserCommandHandler : IConsumer<UpdateUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly NovaTenantDbContext? _tenantDb;
    private readonly ITenantInfo? _tenantInfo;

    public UpdateUserCommandHandler(
        UserManager<User> userManager,
        IDomainEventDispatcher dispatcher,
        NovaTenantDbContext? tenantDb = null,
        ITenantInfo? tenantInfo = null)
    {
        _userManager = userManager;
        _dispatcher = dispatcher;
        _tenantDb = tenantDb;
        _tenantInfo = tenantInfo;
    }

    public async Task Consume(ConsumeContext<UpdateUserCommand> context)
    {
        var command = context.Message;

        var user = await _userManager.FindByIdAsync(command.Id.ToString());
        if (user == null) throw new NovaValidationException("用户不存在");

        if (user.Email != command.Email)
        {
            var existingEmail = await _userManager.FindByEmailAsync(command.Email);
            if (existingEmail != null) throw new NovaValidationException("该邮箱已经被使用");

            var oldEmail = user.Email;
            await _userManager.SetEmailAsync(user, command.Email);
            user.EmailConfirmed = true; // 管理员修改邮箱自动确认

            // BUG-04: 同步更新 GlobalUserTenantMappings
            if (_tenantDb != null && _tenantInfo != null && !string.IsNullOrEmpty(oldEmail))
            {
                var oldMapping = await _tenantDb.GlobalUserTenantMappings
                    .FirstOrDefaultAsync(m => m.Account == oldEmail && m.TenantId == _tenantInfo.Identifier);
                if (oldMapping != null)
                {
                    _tenantDb.GlobalUserTenantMappings.Remove(oldMapping);
                }

                _tenantDb.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
                {
                    Account = command.Email,
                    TenantId = _tenantInfo.Identifier
                });
                await _tenantDb.SaveChangesAsync();
            }
        }

        if (user.PhoneNumber != command.PhoneNumber)
        {
            await _userManager.SetPhoneNumberAsync(user, command.PhoneNumber);
        }

        if (command.IsEnabled) user.Enable();
        else user.Disable();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new NovaValidationException($"用户更新失败: {errors}");
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await _userManager.ResetPasswordAsync(user, token, command.Password);
            if (!pwdResult.Succeeded)
            {
                var errors = string.Join(", ", pwdResult.Errors.Select(e => e.Description));
                throw new NovaValidationException($"用户密码修改失败: {errors}");
            }
        }

        if (command.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            var rolesToRemove = currentRoles.Except(command.Roles).ToList();
            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            var rolesToAdd = command.Roles.Except(currentRoles).ToList();
            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }
        }

        if (command.Permissions != null)
        {
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();
            
            var permissionsToRemove = permissionClaims.Where(c => !command.Permissions.Contains(c.Value)).ToList();
            if (permissionsToRemove.Any())
            {
                await _userManager.RemoveClaimsAsync(user, permissionsToRemove);
            }

            var existingPermValues = permissionClaims.Select(c => c.Value).ToHashSet();
            var permissionsToAdd = command.Permissions.Where(p => !existingPermValues.Contains(p))
                .Select(p => new System.Security.Claims.Claim("Permission", p)).ToList();
            if (permissionsToAdd.Any())
            {
                await _userManager.AddClaimsAsync(user, permissionsToAdd);
            }
        }

        if (command.Menus != null)
        {
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var menuClaims = existingClaims.Where(c => c.Type == "Menu").ToList();
            
            var menusToRemove = menuClaims.Where(c => !command.Menus.Contains(c.Value)).ToList();
            if (menusToRemove.Any())
            {
                await _userManager.RemoveClaimsAsync(user, menusToRemove);
            }

            var existingMenuValues = menuClaims.Select(c => c.Value).ToHashSet();
            var menusToAdd = command.Menus.Where(m => !existingMenuValues.Contains(m))
                .Select(m => new System.Security.Claims.Claim("Menu", m)).ToList();
            if (menusToAdd.Any())
            {
                await _userManager.AddClaimsAsync(user, menusToAdd);
            }
        }

        if (command.Roles != null || command.Permissions != null || command.Menus != null)
        {
            await _dispatcher.PublishAsync(new UserPermissionsUpdatedEvent(user.Id));
        }

        await context.RespondAsync(new UpdateUserResult { Success = true });
    }
}
