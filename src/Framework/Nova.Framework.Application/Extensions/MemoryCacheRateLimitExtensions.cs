using Microsoft.Extensions.Caching.Memory;
using Nova.Contracts.Exceptions;

namespace Nova.Framework.Application.Extensions;

public static class MemoryCacheRateLimitExtensions
{
    /// <summary>
    /// 检查缓存中是否存在对应的 Key，如果存在则抛出验证异常以实现限流。
    /// </summary>
    /// <param name="memoryCache">缓存实例</param>
    /// <param name="key">缓存 Key</param>
    /// <param name="errorMessage">限流错误提示信息</param>
    public static void EnsureRateLimit(this IMemoryCache memoryCache, string key, string errorMessage = "请求过于频繁，请稍后再试。")
    {
        if (memoryCache.TryGetValue(key, out _))
        {
            throw new NovaValidationException(errorMessage);
        }
    }

    /// <summary>
    /// 记录限流状态到缓存中。
    /// </summary>
    /// <param name="memoryCache">缓存实例</param>
    /// <param name="key">缓存 Key</param>
    /// <param name="seconds">限流秒数</param>
    public static void SetRateLimit(this IMemoryCache memoryCache, string key, int seconds = 60)
    {
        memoryCache.Set(key, true, TimeSpan.FromSeconds(seconds));
    }
}
