using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Logging;
using Nova.Modules.Audit.Domain.OperationLogs;
using Nova.Modules.Audit.Infrastructure.Persistence;

namespace Nova.Modules.Audit.Infrastructure.Services;

public class LogProcessorHostedService : BackgroundService
{
    private readonly OperationLogChannel _logChannel;
    private readonly IEntityChangeChannel? _changeChannel;
    private readonly ISanitizerEngine _sanitizerEngine;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogProcessorHostedService> _logger;

    public LogProcessorHostedService(
        OperationLogChannel logChannel,
        ISanitizerEngine sanitizerEngine,
        IServiceScopeFactory scopeFactory,
        ILogger<LogProcessorHostedService> logger,
        IEntityChangeChannel? changeChannel = null)
    {
        _logChannel = logChannel;
        _changeChannel = changeChannel;
        _sanitizerEngine = sanitizerEngine;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[LogProcessor] Global Operation Log background processor started.");

        var logTask = ProcessOperationLogsAsync(stoppingToken);
        var changeTask = ProcessEntityChangesAsync(stoppingToken);

        await Task.WhenAll(logTask, changeTask);
    }

    private async Task ProcessEntityChangesAsync(CancellationToken stoppingToken)
    {
        if (_changeChannel == null) return;

        var buffer = new List<EntityChangeLog>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                await foreach (var changeLog in _changeChannel.ReadAllAsync(timeoutCts.Token))
                {
                    buffer.Add(changeLog);
                    if (buffer.Count >= 50)
                    {
                        await FlushEntityChangesAsync(buffer, stoppingToken);
                        buffer.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // 2 秒定时到达：ReadAllAsync 抛出取消异常，解除阻塞并把 buffer 中的日志主动刷新落库
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[LogProcessor] Unexpected error in ProcessEntityChangesAsync");
            }

            if (buffer.Count > 0)
            {
                await FlushEntityChangesAsync(buffer, stoppingToken);
                buffer.Clear();
            }
        }
    }

    private async Task FlushEntityChangesAsync(List<EntityChangeLog> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var groups = items.GroupBy(x => x.TenantId);

        foreach (var group in groups)
        {
            var tenantId = group.Key;
            var changeLogs = group.ToList();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetService<IMultiTenantStore<NovaTenantInfo>>();
                var contextSetter = scope.ServiceProvider.GetService<IMultiTenantContextSetter>();

                if (store != null && contextSetter != null && !string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenantInfo = await store.GetAsync(tenantId) ?? await store.GetByIdentifierAsync(tenantId);
                    if (tenantInfo != null)
                    {
                        contextSetter.MultiTenantContext = new LogTenantContext { TenantInfo = tenantInfo };
                    }
                }

                var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
                await dbContext.EntityChangeLogs.AddRangeAsync(changeLogs, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogProcessor] Failed to persist {Count} entity change logs for Tenant '{TenantId}'.", changeLogs.Count, tenantId ?? "none");
            }
        }
    }

    private async Task ProcessOperationLogsAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<OperationLogQueueItem>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                await foreach (var item in _logChannel.ReadAllAsync(timeoutCts.Token))
                {
                    buffer.Add(item);
                    if (buffer.Count >= 50)
                    {
                        await FlushLogsAsync(buffer, stoppingToken);
                        buffer.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // 2 秒定时到达：ReadAllAsync 抛出取消异常，解除阻塞并把 buffer 中的日志主动刷新落库
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[LogProcessor] Unexpected error in ProcessOperationLogsAsync");
            }

            if (buffer.Count > 0)
            {
                await FlushLogsAsync(buffer, stoppingToken);
                buffer.Clear();
            }
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

                if (store != null && contextSetter != null && !string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenantInfo = await store.GetAsync(tenantId) ?? await store.GetByIdentifierAsync(tenantId);
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
