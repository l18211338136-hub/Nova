namespace Nova.Modules.Multitenancy.Application.Features;

public record DeleteTenantCommand(string Id);

public record DeleteTenantResult(string TenantId);
