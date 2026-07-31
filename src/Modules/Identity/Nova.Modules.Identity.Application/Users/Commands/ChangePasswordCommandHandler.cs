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

namespace Nova.Modules.Identity.Application.Users.Commands;

public class ChangePasswordCommandHandler : IConsumer<ChangePasswordCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;
    private readonly IDomainEventDispatcher _dispatcher;

    public ChangePasswordCommandHandler(
        IServiceScopeFactory scopeFactory,
        NovaTenantDbContext tenantDb,
        IDomainEventDispatcher dispatcher)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
        _dispatcher = dispatcher;
    }

    public async Task Consume(ConsumeContext<ChangePasswordCommand> context)
    {
        var request = context.Message;

        if (request.CurrentUserId == Guid.Empty)
        {
            throw new NovaValidationException("未获取到当前登录用户信息");
        }

        var tenantId = string.IsNullOrWhiteSpace(request.CurrentTenantId)
            ? NovaIdentityConstants.Tenants.RootTenantId
            : request.CurrentTenantId!;

        var tenantInfo = await _tenantDb.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == tenantId);
        if (tenantInfo == null)
        {
            throw new NovaValidationException("无法定位租户信息");
        }

        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(request.CurrentUserId.ToString());
        if (user == null)
        {
            throw new NovaValidationException("用户不存在");
        }

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            await _dispatcher.PublishAsync(new AuthAuditEvent(
                AuthAuditEventType.PasswordChanged, tenantId, user.Email, user.Id, false, errors));
            throw new NovaValidationException($"修改密码失败: {errors}");
        }

        await _dispatcher.PublishAsync(new AuthAuditEvent(
            AuthAuditEventType.PasswordChanged, tenantId, user.Email, user.Id, true));

        await context.RespondAsync(new ChangePasswordResult { Success = true });
    }
}
