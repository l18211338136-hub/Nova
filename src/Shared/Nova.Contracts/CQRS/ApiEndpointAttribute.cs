using System;

namespace Nova.Contracts.CQRS;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ApiEndpointAttribute : Attribute
{
    public string Method { get; }
    public string Route { get; }
    public Type ResponseType { get; }
    public string Tag { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否仅要求「已登录」即可访问（不校验具体权限点）。
    /// 适用于「用户操作自己的数据」这类端点（查看/修改本人资料、本人偏好）：
    /// 权限点仅播种给 Admin/Root 角色，若这类端点使用 RequirePermission，普通用户会被拒绝。
    /// 若同时标注了 RequirePermissionAttribute，则以权限校验为准（更严格）。
    /// </summary>
    public bool RequireAuthorization { get; set; }

    public ApiEndpointAttribute(string method, string route, Type responseType, string tag = "")
    {
        Method = method;
        Route = route;
        ResponseType = responseType;
        Tag = tag;
    }
}
