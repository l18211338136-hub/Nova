namespace Nova.Contracts.CQRS;

/// <summary>
/// 由框架从 JWT 自动注入、不应出现在对外 API 契约中的命令属性名。
/// </summary>
/// <remarks>
/// 约定来源：<c>Nova.Framework.Web.CQRS.AutoEndpointExtensions</c> 在处理请求时，
/// 会按名称把当前登录用户与租户写入命令对象的同名属性。既然客户端无需（也不应）传递，
/// 生成 OpenAPI 文档时就应把它们从查询参数与请求体 Schema 中剔除，
/// 否则 Orval 等代码生成器会把它们当成必填入参暴露给前端。
/// </remarks>
public static class ServerInjectedProperties
{
    /// <summary>当前登录用户 ID。</summary>
    public const string CurrentUserId = nameof(CurrentUserId);

    /// <summary>当前登录用户所属租户标识。</summary>
    public const string CurrentTenantId = nameof(CurrentTenantId);

    private static readonly HashSet<string> NameSet =
        new(StringComparer.OrdinalIgnoreCase) { CurrentUserId, CurrentTenantId };

    /// <summary>判断属性名是否由服务端注入。</summary>
    public static bool Contains(string propertyName) => NameSet.Contains(propertyName);
}
