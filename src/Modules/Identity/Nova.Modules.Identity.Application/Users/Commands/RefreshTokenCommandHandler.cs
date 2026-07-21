using Nova.Contracts.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class RefreshTokenCommandHandler : IConsumer<RefreshTokenCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ITenantInfo? _tenantInfo;

    public RefreshTokenCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        ITenantInfo? tenantInfo = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _tenantInfo = tenantInfo;
    }

    public async Task Consume(ConsumeContext<RefreshTokenCommand> context)
    {
        var request = context.Message;

        // 1. Read the old access token to extract user ID
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.AccessToken))
        {
            throw new NovaValidationException("无效的 AccessToken 格式");
        }

        var jwtToken = handler.ReadJwtToken(request.AccessToken);
        var userIdString = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new NovaValidationException("无法从 AccessToken 中提取用户信息");
        }

        // 2. Fetch the user
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NovaValidationException("用户不存在");
        }

        // 3. Fetch stored refresh token
        var storedTokenValue = await _userManager.GetAuthenticationTokenAsync(user, "NovaApp", "RefreshToken");
        if (string.IsNullOrEmpty(storedTokenValue))
        {
            throw new NovaValidationException("无有效的刷新令牌，请重新登录");
        }

        // 4. Validate stored refresh token format: {token}|{expiration:O}
        var parts = storedTokenValue.Split('|');
        if (parts.Length != 2)
        {
            throw new NovaValidationException("刷新令牌格式损坏，请重新登录");
        }

        var storedToken = parts[0];
        var expirationString = parts[1];

        if (storedToken != request.RefreshToken)
        {
            throw new NovaValidationException("刷新令牌不匹配");
        }

        if (!DateTimeOffset.TryParse(expirationString, out var expirationDate) || expirationDate <= DateTimeOffset.UtcNow)
        {
            throw new NovaValidationException("刷新令牌已过期，请重新登录");
        }

        // 5. Token Rotation: Generate new token set
        var tenantId = _tenantInfo?.Identifier;
        var tokenResult = _tokenService.GenerateToken(user, tenantId);

        var newRefreshToken = Guid.NewGuid().ToString("N");
        var newRefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        var newStoredTokenValue = $"{newRefreshToken}|{newRefreshTokenExpiry:O}";

        // Overwrite old refresh token in database
        await _userManager.SetAuthenticationTokenAsync(user, "NovaApp", "RefreshToken", newStoredTokenValue);

        await context.RespondAsync(new LoginResult 
        { 
            Token = tokenResult.Token,
            RefreshToken = newRefreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
