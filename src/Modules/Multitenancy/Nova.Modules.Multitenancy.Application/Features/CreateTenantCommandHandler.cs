using Hangfire;
using MassTransit;
using Nova.Modules.Multitenancy.Application.Services;

namespace Nova.Modules.Multitenancy.Application.Features;

public class CreateTenantCommandHandler : IConsumer<CreateTenantCommand>
{
    private readonly ITenantService _tenantService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public CreateTenantCommandHandler(ITenantService tenantService, IBackgroundJobClient backgroundJobClient)
    {
        _tenantService = tenantService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task Consume(ConsumeContext<CreateTenantCommand> context)
    {
        var request = context.Message;

        // 1. 在中央租户信息存储库中创建租户记录（同步）
        var tenantId = await _tenantService.CreateTenantAsync(
            request.Id,
            request.Name,
            request.ConnectionString,
            request.AdminEmail,
            request.Issuer,
            context.CancellationToken).ConfigureAwait(false);

        // 2. 将数据库迁移和数据初始化作为后台任务异步执行
        // 任务完成后会自动发送账号密码到 AdminEmail
        _backgroundJobClient.Enqueue<ITenantService>(
            s => s.MigrateTenantAsync(tenantId, request.AdminPassword, CancellationToken.None));

        // 3. 立即响应前端，无需等待初始化完成
        await context.RespondAsync(new CreateTenantResult(tenantId));
    }
}
