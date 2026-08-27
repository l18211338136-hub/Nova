using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nova.Contracts.DependencyInjection;

namespace Nova.Framework.Authorization.Abac;

public interface IAbacContextAccessor
{
    Guid CurrentUserId { get; }
    Guid? CurrentOrgId { get; }
    string? AbacPoliciesJson { get; }
    int DataScope { get; }
    void SetCurrentAbacPolicy(string? policiesJson, int dataScope, Guid? orgId = null);
}

public class AbacContextAccessor : IAbacContextAccessor, ISingletonDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AbacContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return Guid.Empty;

            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;
        }
    }

    public Guid? CurrentOrgId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(AbacConstants.HttpContextKeys.AbacCurrentOrgId, out var obj) == true && obj is Guid orgId)
            {
                return orgId;
            }
            return null;
        }
    }

    public string? AbacPoliciesJson
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Items[AbacConstants.HttpContextKeys.AbacPoliciesJson] as string;
        }
    }

    public int DataScope
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(AbacConstants.HttpContextKeys.AbacDataScope, out var obj) == true && obj is int scope)
            {
                return scope;
            }
            return 0;
        }
    }

    public void SetCurrentAbacPolicy(string? policiesJson, int dataScope, Guid? orgId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items[AbacConstants.HttpContextKeys.AbacPoliciesJson] = policiesJson;
            httpContext.Items[AbacConstants.HttpContextKeys.AbacDataScope] = dataScope;
            if (orgId != null)
            {
                httpContext.Items[AbacConstants.HttpContextKeys.AbacCurrentOrgId] = orgId;
            }
        }
    }
}
