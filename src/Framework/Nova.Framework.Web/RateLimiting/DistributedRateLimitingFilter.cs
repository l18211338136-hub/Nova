using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nova.Contracts.RateLimiting;
using Nova.Framework.Web.Responses;
using StackExchange.Redis;

namespace Nova.Framework.Web.RateLimiting;

public class DistributedRateLimitingFilter : IEndpointFilter
{
    private readonly DistributedRateLimitAttribute _attribute;

    public DistributedRateLimitingFilter(DistributedRateLimitAttribute attribute)
    {
        _attribute = attribute;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var redis = httpContext.RequestServices?.GetService<IConnectionMultiplexer>();
        if (redis == null)
        {
            return await next(context);
        }

        var db = redis.GetDatabase();
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        
        var prefix = string.IsNullOrWhiteSpace(_attribute.KeyPrefix) 
            ? httpContext.Request.Path.Value 
            : _attribute.KeyPrefix;

        var redisKey = $"ratelimit:{prefix}:{userId}:{clientIp}";
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = _attribute.WindowSeconds * 1000;
        var clearBeforeMs = nowMs - windowMs;

        var transaction = db.CreateTransaction();
        _ = transaction.SortedSetRemoveRangeByScoreAsync(redisKey, 0, clearBeforeMs);
        var countTask = transaction.SortedSetLengthAsync(redisKey);
        _ = transaction.SortedSetAddAsync(redisKey, Guid.NewGuid().ToString(), nowMs);
        _ = transaction.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(_attribute.WindowSeconds + 5));
        
        await transaction.ExecuteAsync();
        var currentCount = await countTask;

        if (currentCount >= _attribute.PermitLimit)
        {
            var logger = httpContext.RequestServices?.GetService<ILogger<DistributedRateLimitingFilter>>();
            logger?.LogWarning("[RateLimit] Client {ClientIp} exceeded rate limit for {Path}. Count: {Count}/{Limit}",
                clientIp, httpContext.Request.Path, currentCount, _attribute.PermitLimit);

            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return ApiResponse.Error("请求过于频繁，请稍后再试", StatusCodes.Status429TooManyRequests);
        }

        return await next(context);
    }
}
