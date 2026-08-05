using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Audit.Application.OperationLogs.Queries;

/// <summary>
/// 获取全局操作日志列表查询。支持按页码、页大小、搜索关键字、HTTP 谓词、状态码、慢日志及脱敏标志过滤。
/// </summary>
[ApiEndpoint("GET", "/api/v1/audit/operation-logs", typeof(PagedResult<OperationLogDto>), "Audit",
    Summary = "获取操作日志列表", Description = "支持按页码、页大小、搜索关键字、HTTP 谓词、状态码、慢日志及脱敏标志过滤操作日志", RequireAuthorization = true)]
public record GetOperationLogsQuery
{
    /// <summary>页码 (默认 1)</summary>
    [Description("页码 (默认 1)")]
    public int Page { get; init; } = 1;

    /// <summary>每页条数 (默认 10)</summary>
    [Description("每页条数 (默认 10)")]
    public int PageSize { get; init; } = 10;

    /// <summary>搜索关键字 (支持匹配请求路径、TraceId、客户端 IP)</summary>
    [Description("搜索关键字 (支持匹配请求路径、TraceId、客户端 IP)")]
    public string? Search { get; init; }

    /// <summary>HTTP 谓词筛选 (如 GET, POST, PUT, DELETE)</summary>
    [Description("HTTP 谓词筛选 (如 GET, POST, PUT, DELETE)")]
    public string? HttpMethod { get; init; }

    /// <summary>HTTP 状态码筛选 (如 200, 400, 500)</summary>
    [Description("HTTP 状态码筛选 (如 200, 400, 500)")]
    public int? StatusCode { get; init; }

    /// <summary>是否仅筛选慢请求</summary>
    [Description("是否仅筛选慢请求")]
    public bool? IsSlowRequest { get; init; }

    /// <summary>是否仅筛选包含脱敏数据的请求</summary>
    [Description("是否仅筛选包含脱敏数据的请求")]
    public bool? HasSanitizedData { get; init; }

    /// <summary>开始时间筛选</summary>
    [Description("开始时间筛选")]
    public DateTime? StartDate { get; init; }

    /// <summary>结束时间筛选</summary>
    [Description("结束时间筛选")]
    public DateTime? EndDate { get; init; }
}

/// <summary>
/// 操作日志数据传输对象
/// </summary>
public record OperationLogDto
{
    [Description("日志主键 ID")]
    public Guid Id { get; init; }

    [Description("链路追踪 Trace ID")]
    public string? TraceId { get; init; }

    [Description("操作用户 ID")]
    public Guid? UserId { get; init; }

    [Description("客户端 IP 地址")]
    public string? ClientIp { get; init; }

    [Description("HTTP 请求谓词")]
    public string? HttpMethod { get; init; }

    [Description("HTTP 请求路径")]
    public string? RequestPath { get; init; }

    [Description("操作/动作名称")]
    public string? ActionName { get; init; }

    [Description("脱敏后的请求载荷 JSON")]
    public string? RequestPayload { get; init; }

    [Description("响应载荷 JSON")]
    public string? ResponsePayload { get; init; }

    [Description("HTTP 状态码")]
    public int? StatusCode { get; init; }

    [Description("执行耗时 (毫秒)")]
    public long? ElapsedMs { get; init; }

    [Description("执行状态 (InProgress / Success / Failed)")]
    public string? Status { get; init; }

    [Description("是否为慢请求")]
    public bool? IsSlowRequest { get; init; }

    [Description("是否包含脱敏数据")]
    public bool? HasSanitizedData { get; init; }

    [Description("错误信息 (仅失败时存在)")]
    public string? ErrorMessage { get; init; }

    [Description("异常堆栈轨迹")]
    public string? ExceptionStackTrace { get; init; }

    [Description("日志记录时间")]
    public DateTime? CreatedAt { get; init; }

    [Description("敏感数据脱敏明细列表")]
    public IEnumerable<SanitizationDetailDto> SanitizationDetails { get; init; } = Array.Empty<SanitizationDetailDto>();
}

/// <summary>
/// 敏感字段脱敏明细数据传输对象
/// </summary>
public record SanitizationDetailDto
{
    [Description("明细主键 ID")]
    public Guid Id { get; init; }

    [Description("关联日志主键 ID")]
    public Guid? LogId { get; init; }

    [Description("被遮蔽/脱敏的敏感字段名称")]
    public string? FieldName { get; init; }

    [Description("脱敏规则标识")]
    public string? MaskedRule { get; init; }

    [Description("脱敏时间")]
    public DateTime? SanitizedAt { get; init; }
}
