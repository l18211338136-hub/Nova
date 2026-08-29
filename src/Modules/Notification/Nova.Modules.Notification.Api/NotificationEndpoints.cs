using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Nova.Framework.Web.Responses;
using Nova.Modules.Notification.Application;
using Nova.Modules.Notification.Domain;
using Nova.Modules.Notification.Infrastructure;
using System.Security.Claims;
using Nova.Contracts.Notification;

namespace Nova.Modules.Notification.Api;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/notifications").RequireAuthorization().WithTags("Notifications");

        group.MapGet("/my", async (INotificationDbContext dbContext, HttpRequest request, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = dbContext.SystemNotifications
                .Where(n => n.ReceiverUserId == userId)
                .Select(n => new SystemNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    NotificationType = n.NotificationType.ToString(),
                    EventCode = n.EventCode,
                    ReferenceId = n.ReferenceId,
                    PayloadJson = n.PayloadJson,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                });

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<SystemNotificationDto>("SystemNotifications");
            var edmModel = builder.GetEdmModel();

            var odataContext = new ODataQueryContext(edmModel, typeof(SystemNotificationDto), null);
            var odataQuery = new ODataQueryOptions<SystemNotificationDto>(odataContext, request);

            var filteredQuery = (IQueryable<SystemNotificationDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);
            long totalCount = await filteredQuery.LongCountAsync(cancellationToken);

            if (odataQuery.Skip != null)
                filteredQuery = filteredQuery.Skip(odataQuery.Skip.Value);
            
            if (odataQuery.Top != null)
                filteredQuery = filteredQuery.Take(odataQuery.Top.Value);
            else
                filteredQuery = filteredQuery.Take(50); // 默认最多50条

            var items = await filteredQuery.ToListAsync(cancellationToken);

            int? top = odataQuery.Top?.Value;
            int? skip = odataQuery.Skip?.Value;
            int? page = (skip.HasValue && top.HasValue && top.Value > 0) ? (skip.Value / top.Value) + 1 : 1;

            var pagedResult = new PagedResult<SystemNotificationDto>
            {
                Total = totalCount,
                Items = items,
                Page = page,
                PageSize = top > 0 ? top : null
            };

            return Results.Ok(ApiResponse<PagedResult<SystemNotificationDto>>.Success(pagedResult));
        })
        .Produces<ApiResponse<PagedResult<SystemNotificationDto>>>(200)
        .WithName("GetMyNotifications")
        .WithSummary("获取通知");

        group.MapPost("/my/{id}/read", async (Guid id, INotificationDbContext dbContext, ClaimsPrincipal user) =>
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Results.Unauthorized();
            }

            var notification = await dbContext.SystemNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverUserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            return Results.Ok(ApiResponse<MarkNotificationAsReadResult>.Success(new MarkNotificationAsReadResult { Success = true }));
        })
        .Produces<ApiResponse<MarkNotificationAsReadResult>>(200)
        .WithName("MarkNotificationAsRead")
        .WithSummary("标记已读");
        
        group.MapPost("/my/read-all", async (INotificationDbContext dbContext, ClaimsPrincipal user) =>
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Results.Unauthorized();
            }

            // 使用 EF Core 批量更新功能 (ExecuteUpdateAsync) 提高性能
            await dbContext.SystemNotifications
                .Where(n => n.ReceiverUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));

            return Results.Ok(ApiResponse<MarkAllNotificationsAsReadResult>.Success(new MarkAllNotificationsAsReadResult { Success = true }));
        })
        .Produces<ApiResponse<MarkAllNotificationsAsReadResult>>(200)
        .WithName("MarkAllNotificationsAsRead")
        .WithSummary("全部已读");

        group.MapPost("/test-send", async (ISystemNotificationService notificationService, ClaimsPrincipal user) =>
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Results.Unauthorized();
            }

            var notificationId = await notificationService.SendToUserAsync(
                userId,
                "TEST_EVENT",
                "这是一条测试通知",
                $"您在 {DateTime.Now:HH:mm:ss} 触发了一条测试系统消息！",
                Contracts.Notification.NotificationType.Info
            );

            return Results.Ok(ApiResponse<SendTestNotificationResult>.Success(new SendTestNotificationResult { NotificationId = notificationId }));
        })
        .Produces<ApiResponse<SendTestNotificationResult>>(200)
        .WithName("SendTestNotification")
        .WithSummary("发送测试");
    }
}
