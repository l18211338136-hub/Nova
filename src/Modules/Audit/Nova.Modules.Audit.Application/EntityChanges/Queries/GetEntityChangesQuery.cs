using System.ComponentModel;
using Nova.Contracts.Audit;
using Nova.Contracts.CQRS;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Audit.Application.EntityChanges.Queries;

[ApiEndpoint("GET", "/api/v1/audit/entity-changes", typeof(PagedResult<EntityChangeLogDto>), "Audit",
    Summary = "变更日志列表", RequireAuthorization = true)]
public record GetEntityChangesQuery
{
    [Description("页码")]
    public int Page { get; init; } = 1;

    [Description("每页条数")]
    public int PageSize { get; init; } = 10;

    [Description("实体类型")]
    public string? EntityType { get; init; }

    [Description("实体标识")]
    public string? EntityId { get; init; }

    [Description("变更类型")]
    public string? ChangeType { get; init; }

    [Description("操作人")]
    public string? OperatorName { get; init; }

    [Description("开始时间")]
    public DateTime? StartDate { get; init; }

    [Description("结束时间")]
    public DateTime? EndDate { get; init; }
}
