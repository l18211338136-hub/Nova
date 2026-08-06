using NSubstitute;
using Nova.Contracts.DistributedLock;
using Nova.Framework.Infrastructure.DistributedLock;
using StackExchange.Redis;

namespace Nova.UnitTests;

public class DistributedLockTests
{
    [Fact]
    public void DistributedLockHandle_ShouldExposeKeyAndDispose()
    {
        // Arrange
        var key = "test:lock:key:123";
        var mockHandle = Substitute.For<Medallion.Threading.IDistributedSynchronizationHandle>();
        var handle = new RedisDistributedLockHandle(key, mockHandle);

        // Assert
        Assert.Equal(key, handle.Key);

        // Act & Assert Dispose
        handle.Dispose();
        mockHandle.Received(1).Dispose();
    }

    [Fact]
    public async Task DistributedLockHandle_ShouldDisposeAsync()
    {
        // Arrange
        var key = "test:lock:key:456";
        var mockHandle = Substitute.For<Medallion.Threading.IDistributedSynchronizationHandle>();
        var handle = new RedisDistributedLockHandle(key, mockHandle);

        // Act
        await handle.DisposeAsync();

        // Assert
        await mockHandle.Received(1).DisposeAsync();
    }

    [Fact]
    public void DistributedLockAttribute_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var attr = new Nova.Contracts.RateLimiting.DistributedRateLimitAttribute();

        // Assert
        Assert.Equal(10, attr.PermitLimit);
        Assert.Equal(60, attr.WindowSeconds);
        Assert.Equal(string.Empty, attr.KeyPrefix);
    }

    [Fact]
    public void DistributedLockAttribute_ShouldInitializeWithCustomValues()
    {
        // Arrange & Act
        var attr = new Nova.Contracts.RateLimiting.DistributedRateLimitAttribute(permitLimit: 5, windowSeconds: 30, keyPrefix: "custom_prefix");

        // Assert
        Assert.Equal(5, attr.PermitLimit);
        Assert.Equal(30, attr.WindowSeconds);
        Assert.Equal("custom_prefix", attr.KeyPrefix);
    }
}
