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
using Nova.Modules.Identity.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Microsoft.AspNetCore.Http;
using Nova.Framework.Web.Helpers;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class RefreshTokenCommandHandler : IConsumer<RefreshTokenCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        IDomainEventDispatcher dispatcher,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
        _dispatcher = dispatcher;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Consume(ConsumeContext<RefreshTokenCommand> context)
    {
        var request = context.Message;
        var clientIp = ClientInfoHelper.GetClientIp(_httpContextAccessor.HttpContext);
        var userAgent = ClientInfoHelper.ParseUserAgent(_httpContextAccessor.HttpContext);

        // 1. Read and validate signature of the old access token (ignoring expiration)
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.AccessToken))
        {
            throw new NovaValidationException("无效的 AccessToken 格式");
        }

        ClaimsPrincipal principal;
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? Environment.GetEnvironmentVariable("NOVA_JWT_SECRET");

        if (!string.IsNullOrWhiteSpace(secretKey))
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // 允许已过期的 AccessToken 用于 RefreshToken 换取新 Token
            };

            try
            {
                principal = handler.ValidateToken(request.AccessToken, validationParameters, out var validatedToken);
                if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new NovaValidationException("AccessToken 签名算法无效");
                }
            }
            catch (Exception ex) when (ex is not NovaValidationException)
            {
                throw new NovaValidationException("AccessToken 签名无效或已被篡改");
            }
        }
        else
        {
            var jwtToken = handler.ReadJwtToken(request.AccessToken);
            principal = new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims, "Jwt"));
        }

        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdString = principal.FindFirst("tenantId")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new NovaValidationException("无法从 AccessToken 中提取用户信息");
        }

        var targetTenantId = string.IsNullOrEmpty(tenantIdString) ? NovaIdentityConstants.Tenants.RootTenantId : tenantIdString;
        var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == targetTenantId);
        if (tenantInfo == null) throw new NovaValidationException("刷新令牌已失效，请重新登录");

        // 开辟新 Scope 并配置租户上下文
        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NovaValidationException("用户不存在");
        }

        // 2. 在可撤销令牌列表中查找匹配的、未吊销、未过期的令牌
        var tokens = await RefreshTokenStore.GetAllAsync(userManager, user);
        var matched = tokens.FirstOrDefault(t =>
            !t.Revoked && t.Token == request.RefreshToken && t.ExpiryUtc > DateTimeOffset.UtcNow);

        if (matched == null)
        {
            await _dispatcher.PublishAsync(new AuthAuditEvent(
                AuthAuditEventType.TokenRefreshed, targetTenantId, user.Email, user.Id, false, "刷新令牌不匹配或已失效", clientIp, userAgent));
            throw new NovaValidationException("刷新令牌不匹配或已失效，请重新登录");
        }

        // 3. Token Rotation：吊销旧令牌，签发新令牌
        matched.Revoked = true;
        var newRefreshToken = Guid.NewGuid().ToString("N");
        var newRefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        tokens.Add(new RefreshTokenEntry
        {
            Token = newRefreshToken,
            ExpiryUtc = newRefreshTokenExpiry,
            Revoked = false,
            CreatedUtc = DateTimeOffset.UtcNow
        });
        await RefreshTokenStore.SetAllAsync(userManager, user, tokens);

        var tokenResult = tokenService.GenerateToken(user, tenantInfo.Identifier);

        await _dispatcher.PublishAsync(new AuthAuditEvent(
            AuthAuditEventType.TokenRefreshed, tenantInfo.Identifier, user.Email, user.Id, true, null, clientIp, userAgent));

        await context.RespondAsync(new LoginResult
        {
            Token = tokenResult.Token,
            RefreshToken = newRefreshToken,
            ExpiresIn = tokenResult.ExpiresIn
        });
    }
}
