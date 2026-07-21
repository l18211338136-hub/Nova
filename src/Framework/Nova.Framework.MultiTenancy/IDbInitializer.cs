namespace Nova.Framework.MultiTenancy;

public interface IDbInitializer
{
    Task MigrateAsync(CancellationToken cancellationToken);
    Task SeedAsync(CancellationToken cancellationToken);
}
