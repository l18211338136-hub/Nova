using System;

using System.ComponentModel;

namespace Nova.Modules.Identity.Application.Users.Queries;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用户名 (登录名)
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 用户邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 用户联系电话 (可选)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 账号是否已启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 账号创建时间 (带时区偏移)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
