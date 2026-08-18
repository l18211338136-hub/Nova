using Nova.Contracts.Caching;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 内存版 INovaCache，供 EmailLogin / Send*Code 等 Handler 在测试中使用。
/// <list type="bullet">
///   <item>线程安全（lock gate）</item>
///   <item>支持值类型的装箱/拆箱（<see cref="GetAsync{T}"/> 会通过 Convert.ChangeType 正确处理 bool?、int? 等）</item>
///   <item>不做过期清理；测试可手动调用 <see cref="RemoveAsync"/> 模拟过期</item>
/// </list>
/// </summary>
public sealed class FakeNovaCache : INovaCache
{
    private readonly Dictionary<string, object?> _store = new();
    private readonly object _gate = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        lock (_gate)
        {
            if (!_store.TryGetValue(key, out var v) || v is null)
                return Task.FromResult(default(T?));

            // 直接匹配（引用类型 string、装箱后的同类型值）
            if (v is T typed)
                return Task.FromResult<T?>(typed);

            // 值类型兼容转换：bool → bool?，int → long? 等
            try
            {
                var underlying = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                var converted = Convert.ChangeType(v, underlying);
                return Task.FromResult<T?>((T)converted);
            }
            catch
            {
                return Task.FromResult(default(T?));
            }
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken token = default)
    {
        lock (_gate)
        {
            _store[key] = value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        lock (_gate)
        {
            _store.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken token = default)
    {
        var existing = GetAsync<T>(key, token).GetAwaiter().GetResult();
        if (existing is not null)
            return Task.FromResult(existing);

        var v = factory(token).GetAwaiter().GetResult();
        SetAsync(key, v, expiration, token).GetAwaiter().GetResult();
        return Task.FromResult(v);
    }
}
