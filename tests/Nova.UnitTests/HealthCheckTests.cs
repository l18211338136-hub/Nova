using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nova.Framework.Web.Extensions;

namespace Nova.UnitTests;

public class HealthCheckTests
{
    [Fact]
    public void AddNovaHealthChecks_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Cache:RedisConnectionString", "127.0.0.1:6379,abortConnect=false"},
            {"ConnectionStrings:DefaultConnection", "Host=localhost;Database=nova_db;Username=postgres;Password=123456"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddLogging();
        services.AddNovaHealthChecks(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }
}
