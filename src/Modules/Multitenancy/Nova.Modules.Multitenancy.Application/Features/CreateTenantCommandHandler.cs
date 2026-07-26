using MassTransit;
using Nova.Modules.Multitenancy.Application.Services;

namespace Nova.Modules.Multitenancy.Application.Features;

public class CreateTenantCommandHandler : IConsumer<CreateTenantCommand>
{
    private readonly ITenantService _tenantService;

    public CreateTenantCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task Consume(ConsumeContext<CreateTenantCommand> context)
    {
        var request = context.Message;
        
        // 1. 在中央租户信息存储库中创建租户记录
        var tenantId = await _tenantService.CreateTenantAsync(
            request.Id,
            request.Name,
            request.ConnectionString,
            request.AdminEmail,
            request.Issuer,
            context.CancellationToken).ConfigureAwait(false);

        // 2. 触发新创建租户的数据库迁移和数据初始化
        // 在包含多个模块的生产环境中，这可能被发送到后台任务队列（如 Hangfire）。
        // 目前，我们同步执行它，以确保租户数据库立即准备就绪。
        await _tenantService.MigrateTenantAsync(tenantId, request.AdminPassword, context.CancellationToken).ConfigureAwait(false);

        await context.RespondAsync(new CreateTenantResult(tenantId));
    }
}
