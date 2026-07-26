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

namespace Nova.Modules.Identity.Application.Users.Commands;

public class LoginCommandHandler : IConsumer<LoginCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ITenantInfo? _tenantInfo;

    public LoginCommandHandler(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ITokenService tokenService,
        ITenantInfo? tenantInfo = null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _tenantInfo = tenantInfo;
    }

    public async Task Consume(ConsumeContext<LoginCommand> context)
    {
        var request = context.Message;
        
        User? user = null;
        if (request.Account.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(request.Account);
        }
        else
        {
            user = await _userManager.FindByNameAsync(request.Account);
        }

        if (user == null)
        {
            throw new NovaValidationException("账号或密码错误");
        }

        var result = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!result)
        {
            throw new NovaValidationException("账号或密码错误");
        }

        var tenantId = _tenantInfo?.Identifier;
        
        // 获取用户的角色和权限 Claims
        var claims = new List<Claim>();
        var roles = await _userManager.GetRolesAsync(user);
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

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                // 仅加入 Menu 和 Permission 相关的 Claim 到 Token
                var authClaims = roleClaims.Where(c => c.Type == "Permission" || c.Type == "Menu");
                foreach(var c in authClaims)
                {
                    // 防止重复添加同样的权限字符串
                    if (!claims.Any(existing => existing.Type == c.Type && existing.Value == c.Value))
                    {
                        claims.Add(c);
                    }
                }
            }
        }

        var tokenResult = _tokenService.GenerateToken(user, tenantId, claims);

        // Generate Refresh Token
        var refreshToken = Guid.NewGuid().ToString("N");
        // Expires in 7 days
        var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        var storedTokenValue = $"{refreshToken}|{refreshTokenExpiry:O}";

        // Store Refresh Token in AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(user, "NovaApp", "RefreshToken", storedTokenValue);

        await context.RespondAsync(new LoginResult 
        { 
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
