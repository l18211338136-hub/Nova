namespace Nova.Contracts.RateLimiting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class DistributedRateLimitAttribute : Attribute
{
    public DistributedRateLimitAttribute(int permitLimit = 10, int windowSeconds = 60, string keyPrefix = "")
    {
        PermitLimit = permitLimit;
        WindowSeconds = windowSeconds;
        KeyPrefix = keyPrefix;
    }

    /// <summary>窗口期内允许的最大请求数</summary>
    public int PermitLimit { get; }

    /// <summary>滑动时间窗口大小 (单位: 秒)</summary>
    public int WindowSeconds { get; }

    /// <summary>限流键前缀</summary>
    public string KeyPrefix { get; }
}
