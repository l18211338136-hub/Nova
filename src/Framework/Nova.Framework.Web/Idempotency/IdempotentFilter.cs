using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nova.Contracts.DistributedLock;
using Nova.Contracts.Idempotency;
using Nova.Framework.Web.Responses;

namespace Nova.Framework.Web.Idempotency;

public class IdempotentFilter : IEndpointFilter
{
    private readonly IdempotentAttribute _attribute;

    public IdempotentFilter(IdempotentAttribute attribute)
    {
        _attribute = attribute;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var lockProvider = httpContext.RequestServices?.GetService<IDistributedLockProvider>();

        if (lockProvider == null)
        {
            return await next(context);
        }

        // 1. 尝试从 Header 提取客户端幂等 ID
        var idempotencyKey = httpContext.Request.Headers[_attribute.HeaderName].ToString();

        // 2. 若 Header 为空，则自动根据 (UserId + ClientIp + Path) 计算默认 Key
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = httpContext.Request.Path.Value ?? string.Empty;

            var rawKey = $"{userId}:{clientIp}:{path}";
            idempotencyKey = ComputeMd5(rawKey);
        }

        var prefix = string.IsNullOrWhiteSpace(_attribute.KeyPrefix) ? "idempotent" : _attribute.KeyPrefix;
        var lockKey = $"{prefix}:{idempotencyKey}";

        // 3. 尝试获取分布式锁（零等待排他）
        var lockHandle = await lockProvider.TryAcquireLockAsync(lockKey, TimeSpan.Zero, httpContext.RequestAborted);

        if (lockHandle == null)
        {
            var logger = httpContext.RequestServices?.GetService<ILogger<IdempotentFilter>>();
            logger?.LogWarning("[Idempotent] Duplicate request intercepted for Key: {LockKey}", lockKey);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return ApiResponse.Error("请求正在处理中，请勿重复提交", StatusCodes.Status409Conflict);
        }

        try
        {
            var result = await next(context);

            // 请求成功完成后，通过 Task.Delay 异步维持锁至 ExpireSeconds 时间后再释放
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(_attribute.ExpireSeconds));
                await lockHandle.DisposeAsync();
            });

            return result;
        }
        catch
        {
            // 若业务代码抛出异常，立即释放锁，允许重试
            await lockHandle.DisposeAsync();
            throw;
        }
    }

    private static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
