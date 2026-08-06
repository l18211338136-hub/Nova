using Nova.Contracts.CQRS;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("DELETE", "/api/identity/users/{id}", typeof(DeleteUserResult), "Users", Summary = "删除用户")]
[RequirePermission("Identity.Users.Delete")]
public record DeleteUserCommand
{
    public Guid Id { get; init; }
}

public record DeleteUserResult
{
    public bool Success { get; init; }
}
