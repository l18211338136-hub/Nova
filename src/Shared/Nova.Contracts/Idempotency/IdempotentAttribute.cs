namespace Nova.Contracts.Idempotency;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class IdempotentAttribute : Attribute
{
    public IdempotentAttribute(int expireSeconds = 5, string keyPrefix = "", string headerName = "X-Idempotency-Key")
    {
        ExpireSeconds = expireSeconds;
        KeyPrefix = keyPrefix;
        HeaderName = headerName;
    }

    /// <summary>防重/幂等锁保留时间 (单位: 秒，默认 5 秒)</summary>
    public int ExpireSeconds { get; }

    /// <summary>自定义键前缀</summary>
    public string KeyPrefix { get; }

    /// <summary>自定义幂等 Header 名称 (默认 X-Idempotency-Key)</summary>
    public string HeaderName { get; }
}
