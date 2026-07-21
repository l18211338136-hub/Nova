namespace Nova.Modules.Multitenancy.Application.Services;

public interface ITenantService
{
    Task<string> CreateTenantAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken);
    Task MigrateTenantAsync(string tenantId, CancellationToken cancellationToken);
}
