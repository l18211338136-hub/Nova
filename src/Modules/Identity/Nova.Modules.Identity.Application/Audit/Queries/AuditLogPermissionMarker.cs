using System.ComponentModel;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.Audit.Queries;

/// <summary>
/// 审计日志查询权限标记。该类没有实际业务逻辑，仅用于让反射收集器发现
/// <see cref="RequirePermissionAttribute"/>，从而把 <c>Identity.AuditLogs.Read</c>
/// 加入系统权限列表并播种给 Admin/Root 角色。
/// </summary>
[Description("审计日志")]
[RequirePermission("Identity.AuditLogs.Read")]
public static class AuditLogPermissionMarker
{
}