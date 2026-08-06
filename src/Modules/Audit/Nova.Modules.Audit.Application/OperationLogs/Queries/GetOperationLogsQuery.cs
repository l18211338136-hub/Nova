using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Audit.Application.OperationLogs.Queries;

[ApiEndpoint("GET", "/api/v1/audit/operation-logs", typeof(PagedResult<OperationLogDto>), "Audit",
    Summary = "操作日志列表", RequireAuthorization = true)]
public record GetOperationLogsQuery
{
    [Description("页码")]
    public int Page { get; init; } = 1;

    [Description("每页条数")]
    public int PageSize { get; init; } = 10;

    [Description("关键字")]
    public string? Search { get; init; }

    [Description("请求方法")]
    public string? HttpMethod { get; init; }

    [Description("状态码")]
    public int? StatusCode { get; init; }

    [Description("仅慢请求")]
    public bool? IsSlowRequest { get; init; }

    [Description("含脱敏数据")]
    public bool? HasSanitizedData { get; init; }

    [Description("开始时间")]
    public DateTime? StartDate { get; init; }

    [Description("结束时间")]
    public DateTime? EndDate { get; init; }
}

public record OperationLogDto
{
    [Description("ID")]
    public Guid Id { get; init; }

    [Description("TraceId")]
    public string? TraceId { get; init; }

    [Description("用户ID")]
    public Guid? UserId { get; init; }

    [Description("客户端IP")]
    public string? ClientIp { get; init; }

    [Description("请求方法")]
    public string? HttpMethod { get; init; }

    [Description("请求路径")]
    public string? RequestPath { get; init; }

    [Description("操作名称")]
    public string? ActionName { get; init; }

    [Description("请求体")]
    public string? RequestPayload { get; init; }

    [Description("响应体")]
    public string? ResponsePayload { get; init; }

    [Description("状态码")]
    public int? StatusCode { get; init; }

    [Description("耗时(ms)")]
    public long? ElapsedMs { get; init; }

    [Description("执行状态")]
    public string? Status { get; init; }

    [Description("是否慢请求")]
    public bool? IsSlowRequest { get; init; }

    [Description("含脱敏数据")]
    public bool? HasSanitizedData { get; init; }

    [Description("错误信息")]
    public string? ErrorMessage { get; init; }

    [Description("异常堆栈")]
    public string? ExceptionStackTrace { get; init; }

    [Description("记录时间")]
    public DateTime? CreatedAt { get; init; }

    [Description("脱敏明细")]
    public IEnumerable<SanitizationDetailDto> SanitizationDetails { get; init; } = Array.Empty<SanitizationDetailDto>();
}

public record SanitizationDetailDto
{
    [Description("ID")]
    public Guid Id { get; init; }

    [Description("日志ID")]
    public Guid? LogId { get; init; }

    [Description("字段名")]
    public string? FieldName { get; init; }

    [Description("脱敏规则")]
    public string? MaskedRule { get; init; }

    [Description("脱敏时间")]
    public DateTime? SanitizedAt { get; init; }
}
