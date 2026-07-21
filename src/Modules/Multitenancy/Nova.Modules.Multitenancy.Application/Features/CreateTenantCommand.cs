namespace Nova.Modules.Multitenancy.Application.Features;

public record CreateTenantCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string AdminPassword,
    string? Issuer);

public record CreateTenantResult(string TenantId);
