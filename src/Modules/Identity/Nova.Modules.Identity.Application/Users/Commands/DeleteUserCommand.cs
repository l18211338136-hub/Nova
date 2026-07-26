using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Users.Commands;

[ApiEndpoint("DELETE", "/api/identity/users/{id}", typeof(DeleteUserResult), "Users", Summary = "删除用户", Description = "管理员删除指定用户")]
public record DeleteUserCommand
{
    public Guid Id { get; init; }
}

public record DeleteUserResult
{
    public bool Success { get; init; }
}
