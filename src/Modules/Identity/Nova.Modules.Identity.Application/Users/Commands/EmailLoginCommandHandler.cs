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

public class EmailLoginCommandHandler : IConsumer<EmailLoginCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;
    private readonly Nova.Contracts.Caching.INovaCache _cache;

    public EmailLoginCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        Nova.Contracts.Caching.INovaCache cache)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<EmailLoginCommand> context)
    {
        var request = context.Message;
        
        // 1. 全局校验验证码
        var cachedCode = await _cache.GetAsync<string>($"LoginCode:{request.Email}");
        if (string.IsNullOrEmpty(cachedCode) || cachedCode != request.Code?.Trim())
        {
            throw new NovaValidationException("验证码错误或已过期");
        }

        // 2. 从全局映射表查找该账号归属的所有租户
        var mappings = await _tenantDb.GlobalUserTenantMappings
            .Where(m => m.Account == request.Email)
            .ToListAsync();

        if (!mappings.Any())
        {
            throw new NovaValidationException("未找到与该邮箱关联的租户账户");
        }

        // 3. 如果前端明确指定了要登录的租户，就过滤掉其他的
        if (!string.IsNullOrEmpty(request.TargetTenantId))
        {
            mappings = mappings.Where(m => m.TenantId == request.TargetTenantId).ToList();
            if (!mappings.Any())
            {
                throw new NovaValidationException("未找到与该邮箱关联的租户账户");
            }
        }

        // 4. 获取有效的租户详情
        var validTenants = new List<NovaTenantInfo>();
        foreach (var mapping in mappings)
        {
            var tenant = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == mapping.TenantId);
            if (tenant != null)
            {
                validTenants.Add(tenant);
            }
        }

        if (validTenants.Count == 0) throw new NovaValidationException("未找到与该邮箱关联的租户账户");

        // 5. 如果账号存在于多个租户下，且前端未指定租户，则返回租户列表让前端选
        if (validTenants.Count > 1)
        {
            await context.RespondAsync(new LoginResult
            {
                RequiresTenantSelection = true,
                AvailableTenants = validTenants.Select(t => new TenantOptionDto { Id = t.Identifier!, Name = t.Name ?? t.Identifier! }).ToList()
            });
            return;
        }

        // 6. 验证码在消费后可以主动清理掉（防重复使用）
        await _cache.RemoveAsync($"LoginCode:{request.Email}");

        // 7. 唯一确定的租户，开始生成 Token
        var targetTenant = validTenants.First();
        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(targetTenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NovaValidationException("未找到与该邮箱关联的租户账户");
        }

        var tenantId = targetTenant.Identifier;
        
        var claims = new List<Claim>();

        // 1. 获取用户直接分配的独立 Claims
        var userClaims = await userManager.GetClaimsAsync(user!);
        claims.AddRange(userClaims.Where(c => c.Type == "Permission" || c.Type == "Menu"));

        // 2. 获取用户角色带来的 Claims
        var roles = await userManager.GetRolesAsync(user);
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

        var tokenResult = tokenService.GenerateToken(user, tenantId, claims);

        // Generate Refresh Token
        var refreshToken = Guid.NewGuid().ToString("N");
        // Expires in 7 days
        var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        var storedTokenValue = $"{refreshToken}|{refreshTokenExpiry:O}";

        // Store Refresh Token in AspNetUserTokens
        await userManager.SetAuthenticationTokenAsync(user, "NovaApp", "RefreshToken", storedTokenValue);

        await context.RespondAsync(new LoginResult 
        { 
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
