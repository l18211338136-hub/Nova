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

namespace Nova.Modules.Identity.Application.Users.Commands;

public class LogoutCommandHandler : IConsumer<LogoutCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;
    private readonly IDomainEventDispatcher _dispatcher;

    public LogoutCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        IDomainEventDispatcher dispatcher)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
        _dispatcher = dispatcher;
    }

    public async Task Consume(ConsumeContext<LogoutCommand> context)
    {
        var request = context.Message;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.AccessToken))
        {
            throw new NovaValidationException("无效的 AccessToken 格式");
        }

        var jwtToken = handler.ReadJwtToken(request.AccessToken);
        var userIdString = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var tenantIdString = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new NovaValidationException("无法从 AccessToken 中提取用户信息");
        }

        var targetTenantId = string.IsNullOrEmpty(tenantIdString) ? NovaIdentityConstants.Tenants.RootTenantId : tenantIdString;
        var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == targetTenantId);
        if (tenantInfo == null) throw new NovaValidationException("刷新令牌已失效，请重新登录");

        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NovaValidationException("用户不存在");
        }

        // 吊销该刷新令牌（不影响该用户的其他会话）
        var tokens = await RefreshTokenStore.GetAllAsync(userManager, user);
        var matched = tokens.FirstOrDefault(t => !t.Revoked && t.Token == request.RefreshToken);
        if (matched != null)
        {
            matched.Revoked = true;
            await RefreshTokenStore.SetAllAsync(userManager, user, tokens);
            await _dispatcher.PublishAsync(new AuthAuditEvent(
                AuthAuditEventType.Logout, tenantInfo.Identifier, user.Email, user.Id, true));
        }
        else
        {
            await _dispatcher.PublishAsync(new AuthAuditEvent(
                AuthAuditEventType.Logout, tenantInfo.Identifier, user.Email, user.Id, false, "刷新令牌不存在或已吊销"));
        }

        await context.RespondAsync(new LogoutResult { Success = true });
    }
}
