using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nova.Contracts.DependencyInjection;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Services;

public class TokenService : ITokenService, ITransientDependency
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, int ExpiresIn) GenerateToken(User user, string? tenantId, IEnumerable<Claim>? additionalClaims = null)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];

        // 密钥治理：优先 User Secrets / appsettings，缺失时回退到环境变量（适用于生产环境）
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            secretKey = Environment.GetEnvironmentVariable("NOVA_JWT_SECRET");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey 未配置：请在 User Secrets 或环境变量 NOVA_JWT_SECRET 中设置。");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("tenantId", tenantId));
        }

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresStr = jwtSettings["ExpiresInMinutes"];
        var expires = int.TryParse(expiresStr, out var e) ? e : 120;

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expires),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires * 60);
    }
}
