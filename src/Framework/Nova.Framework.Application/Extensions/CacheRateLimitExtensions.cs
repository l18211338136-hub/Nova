using System.Threading.Tasks;
using Nova.Contracts.Exceptions;
using Nova.Contracts.Caching;

namespace Nova.Framework.Application.Extensions;

public static class CacheRateLimitExtensions
{
    /// <summary>
    /// 检查缓存中是否存在对应的 Key，如果存在则抛出验证异常以实现限流。
    /// </summary>
    /// <param name="cache">缓存实例</param>
    /// <param name="key">缓存 Key</param>
    /// <param name="errorMessage">限流错误提示信息</param>
    public static async Task EnsureRateLimitAsync(this INovaCache cache, string key, string errorMessage = "请求过于频繁，请稍后再试。")
    {
        var exists = await cache.GetAsync<bool?>(key);
        if (exists.HasValue && exists.Value)
        {
            throw new NovaValidationException(errorMessage);
        }
    }

    /// <summary>
    /// 记录限流状态到缓存中。
    /// </summary>
    /// <param name="cache">缓存实例</param>
    /// <param name="key">缓存 Key</param>
    /// <param name="seconds">限流秒数</param>
    public static async Task SetRateLimitAsync(this INovaCache cache, string key, int seconds = 60)
    {
        await cache.SetAsync(key, true, TimeSpan.FromSeconds(seconds));
    }
}
