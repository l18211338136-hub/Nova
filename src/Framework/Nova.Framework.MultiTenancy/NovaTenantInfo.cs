using Finbuckle.MultiTenant.Abstractions;
using Nova.Framework.Domain.Entities;

namespace Nova.Framework.MultiTenancy;

public class NovaTenantInfo : ITenantInfo, IGlobalEntity
{
    public string Id { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }

    public string? AdminEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ValidUpto { get; set; } = DateTime.UtcNow.AddYears(1);
    public string? Issuer { get; set; }
}
