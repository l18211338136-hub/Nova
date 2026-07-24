using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion;
using Nova.Contracts.Caching;
using Nova.Framework.Infrastructure.Caching;

namespace Nova.Framework.Infrastructure.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddNovaCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheProvider = configuration["Cache:Provider"];
        var redisConnectionString = configuration["Cache:RedisConnectionString"];

        services.AddMemoryCache(); // 必须保留给底层框架或其他依赖 IMemoryCache 的组件（如 EF Core / Identity 等）

        var fusionCacheBuilder = services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5), // 默认缓存 5 分钟
                IsFailSafeEnabled = true,           // 开启防雪崩（Fail-Safe）机制
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
            });

        if (cacheProvider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrEmpty(redisConnectionString))
        {
            // 配置 L2 缓存 (Redis)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
            fusionCacheBuilder.WithRegisteredDistributedCache();
            
            // 配置 Backplane (用于多个节点之间的 L1 缓存同步失效)
            fusionCacheBuilder.WithBackplane(
                new ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis.RedisBackplane(
                    new ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis.RedisBackplaneOptions
                    {
                        Configuration = redisConnectionString
                    })
            );
        }

        return services;
    }
}
