using System.Security.Claims;
using Nova.Contracts.DependencyInjection;

namespace Nova.Contracts.Security;

public interface ICurrentUser : IScopedDependency
{
    Guid? Id { get; }
    string? Name { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<Claim> Claims { get; }
    string[] Roles { get; }
    bool IsInRole(string role);
}
