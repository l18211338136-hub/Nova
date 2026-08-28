using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.TrashBin;

/// <summary>
/// 数据回收权限标记，用于提供前台菜单的权限分组和展示
/// </summary>
[Description("数据回收")]
[RequirePermission("Identity.TrashBin.Read")]
public static class TrashBinPermissionMarker
{
}
