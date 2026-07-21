using Nova.Contracts.Exceptions;
using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class EmailLoginCommandHandler : IConsumer<EmailLoginCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ITenantInfo? _tenantInfo;

    public EmailLoginCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        ITenantInfo? tenantInfo = null)
    {
        _userManager = userManager;
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
        var tokenResult = _tokenService.GenerateToken(user, tenantId);

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
