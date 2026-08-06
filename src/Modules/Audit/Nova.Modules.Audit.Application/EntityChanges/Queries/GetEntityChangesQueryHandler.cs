using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Audit;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.Web.Responses;
using Nova.Modules.Audit.Application.Database;

namespace Nova.Modules.Audit.Application.EntityChanges.Queries;

/// <summary>
/// 实体行级变更日志查询处理器（支持时区规范化与不区分大小写的模糊匹配）
/// </summary>
public class GetEntityChangesQueryHandler : IConsumer<GetEntityChangesQuery>, IScopedDependency
{
    private readonly IAuditDbContext _db;

    public GetEntityChangesQueryHandler(IAuditDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<GetEntityChangesQuery> context)
    {
        var request = context.Message;
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var dbQuery = _db.EntityChangeLogs
            .Include(x => x.PropertyChanges)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.EntityType.ToLower().Contains(entityType));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            var entityId = request.EntityId.Trim();
            dbQuery = dbQuery.Where(x => x.EntityId.Contains(entityId));
        }

        if (!string.IsNullOrWhiteSpace(request.ChangeType))
        {
            var changeType = request.ChangeType.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.ChangeType.ToLower() == changeType);
        }

        if (!string.IsNullOrWhiteSpace(request.OperatorName))
        {
            var opName = request.OperatorName.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.OperatorName != null && x.OperatorName.ToLower().Contains(opName));
        }

        if (request.StartDate.HasValue)
        {
            var utcStart = request.StartDate.Value.ToUniversalTime();
            dbQuery = dbQuery.Where(x => x.CreatedAt >= utcStart);
        }

        if (request.EndDate.HasValue)
        {
            var utcEnd = request.EndDate.Value.ToUniversalTime();
            dbQuery = dbQuery.Where(x => x.CreatedAt <= utcEnd);
        }

        var total = await dbQuery.CountAsync(context.CancellationToken);

        var logs = await dbQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new PagedResult<EntityChangeLogDto>
        {
            Total = total,
            Items = logs.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize
        };

        await context.RespondAsync(response);
    }

    internal static EntityChangeLogDto ToDto(EntityChangeLog log) => new()
    {
        Id = log.Id,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        ChangeType = log.ChangeType,
        OperatorId = log.OperatorId,
        OperatorName = log.OperatorName,
        CreatedAt = log.CreatedAt.DateTime,
        PropertyChanges = log.PropertyChanges.Select(ToPropertyChangeDto).ToList()
    };

    internal static EntityPropertyChangeDto ToPropertyChangeDto(EntityPropertyChange p) => new()
    {
        Id = p.Id,
        PropertyName = p.PropertyName,
        PropertyDisplayName = p.PropertyDisplayName,
        OriginalValue = p.OriginalValue,
        NewValue = p.NewValue
    };
}
