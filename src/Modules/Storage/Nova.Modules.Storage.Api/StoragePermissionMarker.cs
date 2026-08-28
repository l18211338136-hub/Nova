using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Storage.Api;

/// <summary>
/// 存储模块权限标记，用于提供前台菜单的权限分组和展示
/// </summary>
[Description("文件存储")]
[RequirePermission("Storage.Files.Read")]
public static class StoragePermissionMarker
{
}
