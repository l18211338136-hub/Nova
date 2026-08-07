using System.ComponentModel;
using Nova.Contracts.CQRS;

namespace Nova.Modules.Identity.Application.Profile.Commands;

[ApiEndpoint("PUT", "/api/identity/profile", typeof(UpdateProfileResult), "Profile", Summary = "修改资料", RequireAuthorization = true)]
public record UpdateProfileCommand
{
    public Guid CurrentUserId { get; init; }
    public string? CurrentTenantId { get; init; }

    [Description("昵称")]
    public string? NickName { get; init; }

    [Description("简介")]
    public string? Bio { get; init; }

    [Description("头像地址")]
    public string? AvatarUrl { get; init; }

    [Description("头像物理文件Id")]
    public Guid? AvatarFileId { get; init; }

    [Description("手机号")]
    public string? PhoneNumber { get; init; }
}

public record UpdateProfileResult
{
    [Description("是否成功")]
    public bool Success { get; init; }
}
