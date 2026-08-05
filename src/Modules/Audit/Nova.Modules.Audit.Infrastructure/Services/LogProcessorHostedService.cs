using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Logging;
using Nova.Modules.Audit.Domain.OperationLogs;
using Nova.Modules.Audit.Infrastructure.Persistence;

namespace Nova.Modules.Audit.Infrastructure.Services;

public class LogProcessorHostedService : BackgroundService
{
    private readonly OperationLogChannel _logChannel;
    private readonly ISanitizerEngine _sanitizerEngine;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogProcessorHostedService> _logger;

    public LogProcessorHostedService(
        IOperationLogChannel logChannel,
        ISanitizerEngine sanitizerEngine,
        IServiceScopeFactory scopeFactory,
        ILogger<LogProcessorHostedService> logger)
    {
        _logChannel = (OperationLogChannel)logChannel;
        _sanitizerEngine = sanitizerEngine;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LogProcessor] Global Operation Log background processor started.");

        var buffer = new List<OperationLogQueueItem>();
        var lastFlushTime = DateTime.UtcNow;

        await foreach (var item in _logChannel.ReadAllAsync(stoppingToken))
        {
            buffer.Add(item);

            if (buffer.Count >= 50 || (DateTime.UtcNow - lastFlushTime).TotalSeconds >= 2)
            {
                await FlushLogsAsync(buffer, stoppingToken);
                buffer.Clear();
                lastFlushTime = DateTime.UtcNow;
            }
        }

        if (buffer.Count > 0)
        {
            await FlushLogsAsync(buffer, CancellationToken.None);
        }
    }

    private async Task FlushLogsAsync(List<OperationLogQueueItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var groups = items.GroupBy(x => x.TenantId);

        foreach (var group in groups)
        {
            var tenantId = group.Key;
            var operationLogs = new List<OperationLog>();

            foreach (var queueItem in group)
            {
                var req = queueItem.Request;
                var domainLog = OperationLog.Create(
                    traceId: req.TraceId,
                    userId: req.UserId,
                    clientIp: req.ClientIp,
                    httpMethod: req.HttpMethod,
                    requestPath: req.RequestPath,
                    actionName: req.ActionName
                );

                // 请求载荷与响应载荷安全自动脱敏
                domainLog.SetAndSanitizeRequestPayload(req.RequestPayload, _sanitizerEngine);
                domainLog.SetAndSanitizeResponsePayload(req.ResponsePayload, _sanitizerEngine);

                if (req.IsSuccess)
                {
                    domainLog.MarkAsSuccess(req.StatusCode, req.ElapsedMs);
                }
                else
                {
                    var ex = new Exception(req.ErrorMessage ?? "Request processing failed.");
                    domainLog.MarkAsFailed(ex, req.StatusCode);
                }

                operationLogs.Add(domainLog);
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetService<IMultiTenantStore<NovaTenantInfo>>();
                var contextSetter = scope.ServiceProvider.GetService<IMultiTenantContextSetter>();

                if (store != null && contextSetter != null)
                {
                    NovaTenantInfo? tenantInfo = null;

                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        tenantInfo = await store.GetAsync(tenantId) ?? await store.GetByIdentifierAsync(tenantId);
                    }

                    // 若未显式匹配出租户，回退到宿主 root 租户挂载，确保 Finbuckle 不会抛出 TenantInfo is null 异常
                    if (tenantInfo == null)
                    {
                        tenantInfo = await store.GetAsync("root") ?? await store.GetByIdentifierAsync("root");
                    }

                    if (tenantInfo != null)
                    {
                        contextSetter.MultiTenantContext = new LogTenantContext { TenantInfo = tenantInfo };
                    }
                }

                var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
                await dbContext.OperationLogs.AddRangeAsync(operationLogs, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogProcessor] Failed to persist {Count} operation logs for Tenant '{TenantId}'", operationLogs.Count, tenantId ?? "none");
            }
        }
    }
}

public class LogTenantContext : IMultiTenantContext<NovaTenantInfo>
{
    public NovaTenantInfo? TenantInfo { get; set; }
    ITenantInfo? IMultiTenantContext.TenantInfo { get => TenantInfo; init => TenantInfo = (NovaTenantInfo?)value; }
    public StrategyInfo? StrategyInfo { get; init; }
    public StoreInfo<NovaTenantInfo>? StoreInfo { get; set; }
    public bool IsResolved => true;
}
