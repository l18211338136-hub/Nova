using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Users.Commands;

/// <summary>
/// 可撤销的刷新令牌存储。以 JSON 列表形式存于 ASP.NET Identity 的 AuthenticationToken 存储，
/// 支持同一用户多端登录、按需吊销单个令牌（登出）、以及轮换时标记旧令牌失效。
/// </summary>
public class RefreshTokenEntry
{
    public string Token { get; set; } = default!;
    public DateTimeOffset ExpiryUtc { get; set; }
    public bool Revoked { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

public static class RefreshTokenStore
{
    private const string Provider = "NovaApp";
    private const string Name = "RefreshTokens";

    public static async Task<List<RefreshTokenEntry>> GetAllAsync(UserManager<User> um, User user)
    {
        var raw = await um.GetAuthenticationTokenAsync(user, Provider, Name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<RefreshTokenEntry>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<RefreshTokenEntry>>(raw) ?? new List<RefreshTokenEntry>();
        }
        catch (JsonException)
        {
            return new List<RefreshTokenEntry>();
        }
    }

    public static async Task SetAllAsync(UserManager<User> um, User user, List<RefreshTokenEntry> list)
    {
        var raw = JsonSerializer.Serialize(list);
        await um.SetAuthenticationTokenAsync(user, Provider, Name, raw);
    }

    public static async Task AddAsync(UserManager<User> um, User user, RefreshTokenEntry entry)
    {
        var list = await GetAllAsync(um, user);
        list.Add(entry);
        await SetAllAsync(um, user, list);
    }
}
