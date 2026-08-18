using Microsoft.IdentityModel.Tokens;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 测试用 ITokenService：生成真实可解析的 JWT（含 NameIdentifier / tenantId claim），
/// 以便 RefreshToken 等 Handler 能用 JwtSecurityTokenHandler 正常读取。不做真实签名校验。
/// </summary>
public sealed class FakeTokenService : ITokenService
{
    /// <summary>固定测试密钥，长度满足 HMAC-SHA256 最低要求（≥32 字节）。</summary>
    public const string TestSecretKey = "this-is-a-test-secret-key-1234567890abcd";

    public (string Token, int ExpiresIn) GenerateToken(
        User user, string? tenantId, IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrEmpty(tenantId))
            claims.Add(new Claim("tenantId", tenantId));

        if (additionalClaims != null)
            claims.AddRange(additionalClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), 120 * 60);
    }
}
