namespace Nova.Modules.Multitenancy.Application.Services;

public interface ITenantService
{
    Task<string> CreateTenantAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken);
    Task UpdateTenantAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, bool isActive, DateTime validUpto, CancellationToken cancellationToken);
    Task DeleteTenantAsync(string id, CancellationToken cancellationToken);
    Task MigrateTenantAsync(string tenantId, string? adminPassword = null, CancellationToken cancellationToken = default);
}
