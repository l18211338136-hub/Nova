using Nova.Contracts.Exceptions;
using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class LoginCommandHandler : IConsumer<LoginCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public LoginCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
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

        // 3. 密码探针：挨个租户尝试密码验证
        foreach (var mapping in mappings)
        {
            var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == mapping.TenantId);
            if (tenantInfo == null) continue;

            using var scope = _scopeFactory.CreateScope();
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

            var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            User? tempUser = request.Account.Contains('@') 
                ? await um.FindByEmailAsync(request.Account) 
                : await um.FindByNameAsync(request.Account);

            if (tempUser != null && await um.CheckPasswordAsync(tempUser, request.Password))
            {
                validTenants.Add(tenantInfo);
            }
        }

        if (validTenants.Count == 0)
        {
            throw new NovaValidationException("账号或密码错误");
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

        User? user = request.Account.Contains('@') 
            ? await userManager.FindByEmailAsync(request.Account) 
            : await userManager.FindByNameAsync(request.Account);

        var tenantId = targetTenant.Identifier;
        
        // 获取用户的角色和权限 Claims
        var claims = new List<Claim>();
        var roles = await userManager.GetRolesAsync(user!);
        foreach (var roleName in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));

            // 如果是超级管理员，动态赋予绝对通配符，无需查库
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
                foreach(var c in authClaims)
                {
                    if (!claims.Any(existing => existing.Type == c.Type && existing.Value == c.Value))
                    {
                        claims.Add(c);
                    }
                }
            }
        }

        var tokenResult = tokenService.GenerateToken(user!, tenantId, claims);

        var refreshToken = Guid.NewGuid().ToString("N");
        var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        var storedTokenValue = $"{refreshToken}|{refreshTokenExpiry:O}";

        await userManager.SetAuthenticationTokenAsync(user!, "NovaApp", "RefreshToken", storedTokenValue);

        await context.RespondAsync(new LoginResult 
        { 
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
