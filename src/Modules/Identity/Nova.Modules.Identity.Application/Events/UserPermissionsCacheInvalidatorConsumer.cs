using MassTransit;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Caching;

namespace Nova.Modules.Identity.Application.Events;

/// <summary>
/// 监听 UserPermissionsUpdatedEvent 领域事件，自动触发 INovaCache 中的用户权限缓存失效。
/// </summary>
public class UserPermissionsCacheInvalidatorConsumer : IConsumer<UserPermissionsUpdatedEvent>
{
    private readonly INovaCache _cache;
    private readonly ILogger<UserPermissionsCacheInvalidatorConsumer> _logger;

    public UserPermissionsCacheInvalidatorConsumer(INovaCache cache, ILogger<UserPermissionsCacheInvalidatorConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserPermissionsUpdatedEvent> context)
    {
        var userId = context.Message.UserId;
        var cacheKey = $"Auth:Permissions:{userId}";

        await _cache.RemoveAsync(cacheKey);

        _logger.LogInformation("[PermissionCache] Successfully invalidated permission cache for user {UserId}", userId);
    }
}
