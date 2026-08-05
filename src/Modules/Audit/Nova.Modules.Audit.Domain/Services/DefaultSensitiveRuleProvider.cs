using Nova.Contracts.DependencyInjection;

namespace Nova.Modules.Audit.Domain.Services;

public class DefaultSensitiveRuleProvider : ISensitiveRuleProvider, ISingletonDependency
{
    private static readonly HashSet<string> DefaultKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pass", "pwd", "secret", "token", "accesstoken", "refreshtoken",
        "apikey", "authorization", "privatekey", "creditcard", "cardnumber"
    };

    private HashSet<string> _currentKeys;

    public DefaultSensitiveRuleProvider(IEnumerable<string>? customKeys = null)
    {
        _currentKeys = new HashSet<string>(DefaultKeys, StringComparer.OrdinalIgnoreCase);
        if (customKeys != null)
        {
            foreach (var key in customKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _currentKeys.Add(key.Trim());
                }
            }
        }
    }

    public IReadOnlySet<string> GetSensitiveKeys() => _currentKeys;

    /// <summary>
    /// 支持后续从数据库/缓存动态更新脱敏词库（无需重启服务）
    /// </summary>
    public void UpdateRules(IEnumerable<string> newKeys)
    {
        var updated = new HashSet<string>(DefaultKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var key in newKeys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                updated.Add(key.Trim());
            }
        }
        Interlocked.Exchange(ref _currentKeys, updated);
    }
}
