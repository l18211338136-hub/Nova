using System;

namespace Nova.Modules.Multitenancy.Application.Features;

public record UpdateTenantCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string? Issuer,
    bool IsActive,
    DateTime ValidUpto
);

public record UpdateTenantResult(string TenantId);
