using Nova.Framework.Domain.Entities;
using Nova.Modules.Audit.Domain.Services;

namespace Nova.Modules.Audit.Domain.OperationLogs;

/// <summary>
/// 操作日志聚合根（DDD 充血模型，所有字段支持 Nullable，由 Finbuckle 隐式 Shadow Property 管理）
/// </summary>
public class OperationLog : Entity<Guid>
{
    public string? TraceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? ClientIp { get; private set; }
    public string? HttpMethod { get; private set; }
    public string? RequestPath { get; private set; }
    public string? ActionName { get; private set; }

    // Payload 存储
    public string? RequestPayload { get; private set; }
    public string? ResponsePayload { get; private set; }

    // 执行状态与耗时
    public int? StatusCode { get; private set; }
    public long? ElapsedMs { get; private set; }
    public ExecutionStatus? Status { get; private set; }
    public bool? IsSlowRequest { get; private set; }

    // 脱敏与异常信息
    public bool? HasSanitizedData { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ExceptionStackTrace { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    // 导航属性：关联的敏感数据脱敏记录
    private readonly List<SanitizationDetail> _sanitizationDetails = new();
    public IReadOnlyCollection<SanitizationDetail> SanitizationDetails => _sanitizationDetails.AsReadOnly();

    private OperationLog() { }

    public static OperationLog Create(
        string? traceId,
        Guid? userId,
        string? clientIp,
        string? httpMethod,
        string? requestPath,
        string? actionName = null)
    {
        return new OperationLog
        {
            Id = Guid.CreateVersion7(),
            TraceId = traceId,
            UserId = userId,
            ClientIp = clientIp,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            ActionName = actionName,
            Status = ExecutionStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 充血方法：设置并自动脱敏 RequestPayload，记录脱敏明细
    /// </summary>
    public void SetAndSanitizeRequestPayload(string? rawPayload, ISanitizerEngine? sanitizerEngine)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            RequestPayload = rawPayload;
            return;
        }

        if (sanitizerEngine == null)
        {
            RequestPayload = rawPayload;
            return;
        }

        var result = sanitizerEngine.Sanitize(rawPayload);
        RequestPayload = result.SanitizedText;

        if (result.MaskedFields.Count > 0)
        {
            HasSanitizedData = true;
            foreach (var field in result.MaskedFields)
            {
                _sanitizationDetails.Add(SanitizationDetail.Create(Id, field.FieldName, field.MaskedRule));
            }
        }
    }

    /// <summary>
    /// 充血方法：设置并自动脱敏 ResponsePayload
    /// </summary>
    public void SetAndSanitizeResponsePayload(string? rawPayload, ISanitizerEngine? sanitizerEngine)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            ResponsePayload = rawPayload;
            return;
        }

        if (sanitizerEngine == null)
        {
            ResponsePayload = rawPayload;
            return;
        }

        var result = sanitizerEngine.Sanitize(rawPayload);
        ResponsePayload = result.SanitizedText;

        if (result.MaskedFields.Count > 0)
        {
            HasSanitizedData = true;
            foreach (var field in result.MaskedFields)
            {
                _sanitizationDetails.Add(SanitizationDetail.Create(Id, field.FieldName, field.MaskedRule));
            }
        }
    }

    /// <summary>
    /// 充血方法：标记请求成功完成
    /// </summary>
    public void MarkAsSuccess(int? statusCode, long? elapsedMs, long slowThresholdMs = 500)
    {
        StatusCode = statusCode;
        ElapsedMs = elapsedMs;
        Status = ExecutionStatus.Success;
        IsSlowRequest = elapsedMs.HasValue && elapsedMs.Value >= slowThresholdMs;
    }

    /// <summary>
    /// 充血方法：标记请求失败并记录异常堆栈
    /// </summary>
    public void MarkAsFailed(Exception? exception, int? statusCode = 500)
    {
        StatusCode = statusCode;
        Status = ExecutionStatus.Failed;
        ErrorMessage = exception?.Message;
        ExceptionStackTrace = exception?.StackTrace;
    }
}
