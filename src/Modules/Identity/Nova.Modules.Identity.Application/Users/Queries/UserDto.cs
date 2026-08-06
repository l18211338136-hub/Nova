using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Users.Queries;

[RequirePermission("Identity.Users.Read")]
[Description("用户管理")]
public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
