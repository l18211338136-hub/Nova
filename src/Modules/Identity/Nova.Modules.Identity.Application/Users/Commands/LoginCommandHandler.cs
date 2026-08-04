using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Events;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using System.Security.Claims;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class LoginCommandHandler : IConsumer<LoginCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;
    private readonly IDomainEventDispatcher _dispatcher;

    public LoginCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        IDomainEventDispatcher dispatcher)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
        _dispatcher = dispatcher;
    }

    public async Task Consume(ConsumeContext<LoginCommand> context)
    {
        var request = context.Message;
        // 1. 从全局映射表查找该账号归属的所有租户
        var mappings = await _tenantDb.GlobalUserTenantMappings
            .Where(m => m.Account == request.Account)
            .ToListAsync();

        if (!mappings.Any())
        {
            throw new NovaValidationException("账号或密码错误");
        }

        // 2. 如果前端明确指定了要登录的租户，就过滤掉其他的
        if (!string.IsNullOrEmpty(request.TargetTenantId))
        {
            mappings = mappings.Where(m => m.TenantId == request.TargetTenantId).ToList();
            if (!mappings.Any())
            {
                throw new NovaValidationException("账号或密码错误");
            }
        }

        var validTenants = new List<NovaTenantInfo>();
        var accountLocked = false;

        // 根据输入判定用户标识类型：邮箱 / 手机号 / 用户名，分别用对应的 UserManager 查找方法
        static bool IsPhoneNumber(string account)
            => System.Text.RegularExpressions.Regex.IsMatch(account, @"^\+?\d{6,15}$");

        static async Task<User?> FindUserByIdentifierAsync(UserManager<User> um, string account)
        {
            if (account.Contains('@'))
                return await um.FindByEmailAsync(account);
            if (IsPhoneNumber(account))
                return await um.Users.FirstOrDefaultAsync(u => u.PhoneNumber == account);
            return await um.FindByNameAsync(account);
        }


        // 3. 密码探针：挨个租户尝试密码验证（并应用账号锁定策略）
        foreach (var mapping in mappings)
        {
            var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == mapping.TenantId);
            if (tenantInfo == null) continue;

            using var scope = _scopeFactory.CreateScope();
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

            var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            User? tempUser = await FindUserByIdentifierAsync(um, request.Account);

            if (tempUser == null) continue;

            // 3a. 账号锁定检查（防暴力破解）
            if (await um.IsLockedOutAsync(tempUser))
            {
                accountLocked = true;
                await _dispatcher.PublishAsync(new AuthAuditEvent(
                    AuthAuditEventType.LoginFailed, tenantInfo.Identifier, request.Account, tempUser.Id, false, "账号已被锁定"));
                continue;
            }

            if (await um.CheckPasswordAsync(tempUser, request.Password))
            {
                await um.ResetAccessFailedCountAsync(tempUser);
                validTenants.Add(tenantInfo);
            }
            else
            {
                await um.AccessFailedAsync(tempUser);
                await _dispatcher.PublishAsync(new AuthAuditEvent(
                    AuthAuditEventType.LoginFailed, tenantInfo.Identifier, request.Account, tempUser.Id, false, "密码错误"));
            }
        }

        if (validTenants.Count == 0)
        {
            // 全部因锁定失败 -> 提示锁定；否则提示账号或密码错误
            throw new NovaValidationException(accountLocked
                ? "账号已被锁定，请稍后再试"
                : "账号或密码错误");
        }

        // 4. 如果密码在多个租户下都正确，且前端未指定租户，则返回租户列表让前端选
        if (validTenants.Count > 1)
        {
            await context.RespondAsync(new LoginResult
            {
                RequiresTenantSelection = true,
                AvailableTenants = validTenants.Select(t => new TenantOptionDto { Id = t.Identifier!, Name = t.Name ?? t.Identifier! }).ToList()
            });
            return;
        }

        // 5. 唯一确定的租户，开始生成 Token
        var targetTenant = validTenants.First();
        using var finalScope = _scopeFactory.CreateScope();
        var finalSetter = finalScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        finalSetter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(targetTenant);

        var userManager = finalScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = finalScope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var tokenService = finalScope.ServiceProvider.GetRequiredService<ITokenService>();

        User? user = await FindUserByIdentifierAsync(userManager, request.Account);

        var tenantId = targetTenant.Identifier;

        // 获取用户的角色和权限 Claims
        var claims = new List<Claim>();

        var userClaims = await userManager.GetClaimsAsync(user!);
        claims.AddRange(userClaims.Where(c => c.Type == "Permission" || c.Type == "Menu"));

        var roles = await userManager.GetRolesAsync(user!);
        foreach (var roleName in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));

            if (roleName == NovaIdentityConstants.Roles.Root)
            {
                if (!claims.Any(c => c.Type == "Permission" && c.Value == "*"))
                    claims.Add(new Claim("Permission", "*"));
                if (!claims.Any(c => c.Type == "Menu" && c.Value == "*"))
                    claims.Add(new Claim("Menu", "*"));
                continue;
            }

            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await roleManager.GetClaimsAsync(role);
                var authClaims = roleClaims.Where(c => c.Type == "Permission" || c.Type == "Menu");
                foreach (var c in authClaims)
                {
                    if (!claims.Any(existing => existing.Type == c.Type && existing.Value == c.Value))
                    {
                        claims.Add(c);
                    }
                }
            }
        }

        var tokenResult = tokenService.GenerateToken(user!, tenantId, claims);

        // 6. 生成可撤销的刷新令牌列表（支持多端、登出吊销、刷新轮换）
        var refreshToken = Guid.NewGuid().ToString("N");
        var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        await RefreshTokenStore.AddAsync(userManager, user!, new RefreshTokenEntry
        {
            Token = refreshToken,
            ExpiryUtc = refreshTokenExpiry,
            Revoked = false,
            CreatedUtc = DateTimeOffset.UtcNow
        });

        await _dispatcher.PublishAsync(new AuthAuditEvent(
            AuthAuditEventType.LoginSuccess, tenantId, request.Account, user!.Id, true, "密码登录"));

        await context.RespondAsync(new LoginResult
        {
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
