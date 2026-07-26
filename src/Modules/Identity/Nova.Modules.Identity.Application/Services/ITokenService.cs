using Nova.Contracts.DependencyInjection;
using Nova.Modules.Identity.Domain.Users;

using System.Security.Claims;

namespace Nova.Modules.Identity.Application.Services;

public interface ITokenService : IScopedDependency
{
    (string Token, int ExpiresIn) GenerateToken(User user, string? tenantId, IEnumerable<Claim>? additionalClaims = null);
}
