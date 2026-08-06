using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Web.Responses;
using Nova.Modules.Audit.Application.Database;
using Nova.Modules.Audit.Domain.OperationLogs;

namespace Nova.Modules.Audit.Application.OperationLogs.Queries;

/// <summary>
/// 全局操作日志查询处理器
/// </summary>
public class GetOperationLogsQueryHandler : IConsumer<GetOperationLogsQuery>, IScopedDependency
{
    private readonly IAuditDbContext _db;

    public GetOperationLogsQueryHandler(IAuditDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<GetOperationLogsQuery> context)
    {
        var request = context.Message;
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var dbQuery = _db.OperationLogs
            .Include(x => x.SanitizationDetails)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            dbQuery = dbQuery.Where(x =>
                (x.RequestPath != null && x.RequestPath.Contains(search)) ||
                (x.TraceId != null && x.TraceId.Contains(search)) ||
                (x.ClientIp != null && x.ClientIp.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.HttpMethod))
        {
            var method = request.HttpMethod.Trim().ToUpper();
            dbQuery = dbQuery.Where(x => x.HttpMethod != null && x.HttpMethod.ToUpper() == method);
        }

        if (request.StatusCode.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.StatusCode == request.StatusCode.Value);
        }

        if (request.IsSlowRequest.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.IsSlowRequest == request.IsSlowRequest.Value);
        }

        if (request.HasSanitizedData.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.HasSanitizedData == request.HasSanitizedData.Value);
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

        var response = new PagedResult<OperationLogDto>
        {
            Total = total,
            Items = logs.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize
        };

        await context.RespondAsync(response);
    }

    internal static OperationLogDto ToDto(OperationLog log) => new()
    {
        Id = log.Id,
        TraceId = log.TraceId,
        UserId = log.UserId,
        ClientIp = log.ClientIp,
        HttpMethod = log.HttpMethod,
        RequestPath = log.RequestPath,
        ActionName = log.ActionName,
        RequestPayload = log.RequestPayload,
        ResponsePayload = log.ResponsePayload,
        StatusCode = log.StatusCode,
        ElapsedMs = log.ElapsedMs,
        Status = log.Status?.ToString(),
        IsSlowRequest = log.IsSlowRequest,
        HasSanitizedData = log.HasSanitizedData,
        ErrorMessage = log.ErrorMessage,
        ExceptionStackTrace = log.ExceptionStackTrace,
        CreatedAt = log.CreatedAt,
        SanitizationDetails = log.SanitizationDetails.Select(ToSanitizationDetailDto).ToList()
    };

    internal static SanitizationDetailDto ToSanitizationDetailDto(SanitizationDetail detail) => new()
    {
        Id = detail.Id,
        LogId = detail.LogId,
        FieldName = detail.FieldName,
        MaskedRule = detail.MaskedRule,
        SanitizedAt = detail.SanitizedAt
    };
}
