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

public class EmailLoginCommandHandler : IConsumer<EmailLoginCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ITenantInfo? _tenantInfo;

    public EmailLoginCommandHandler(
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

    public async Task Consume(ConsumeContext<EmailLoginCommand> context)
    {
        var request = context.Message;
        
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NovaValidationException("验证码错误或已过期");
        }

        // 使用 Identity 自带验证方法验证 TOTP Token
        var result = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", request.Code?.Trim());
        if (!result)
        {
            throw new NovaValidationException("验证码错误或已过期");
        }

        var tenantId = _tenantInfo?.Identifier;
        
        var claims = new List<Claim>();
        var roles = await _userManager.GetRolesAsync(user);
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

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
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
