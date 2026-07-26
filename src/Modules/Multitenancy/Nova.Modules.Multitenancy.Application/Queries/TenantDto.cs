using System;
using Nova.Contracts.Security;

namespace Nova.Modules.Multitenancy.Application.Queries;

[RequirePermission("Multitenancy.Tenants.Read")]
[System.ComponentModel.Description("租户管理")]
public record TenantDto
{
    public string Id { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ConnectionString { get; init; }
    public string? AdminEmail { get; init; }
    public bool IsActive { get; init; }
    public DateTime ValidUpto { get; init; }
    public string? Issuer { get; init; }
}
