using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nova.Contracts.DistributedLock;
using Nova.Contracts.Idempotency;
using Nova.Framework.Web.Idempotency;
using Nova.Framework.Web.Responses;

namespace Nova.UnitTests;

public class IdempotentTests
{
    [Fact]
    public void IdempotentAttribute_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var attr = new IdempotentAttribute();

        // Assert
        Assert.Equal(5, attr.ExpireSeconds);
        Assert.Equal(string.Empty, attr.KeyPrefix);
        Assert.Equal("X-Idempotency-Key", attr.HeaderName);
    }

    [Fact]
    public async Task IdempotentFilter_ShouldPassThrough_WhenDistributedLockNotAvailable()
    {
        // Arrange
        var attr = new IdempotentAttribute(5);
        var filter = new IdempotentFilter(attr);

        var httpContext = new DefaultHttpContext();
        var invocationContext = Substitute.For<EndpointFilterInvocationContext>();
        invocationContext.HttpContext.Returns(httpContext);

        var nextCalled = false;
        EndpointFilterDelegate next = (ctx) =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("success");
        };

        // Act
        var result = await filter.InvokeAsync(invocationContext, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task IdempotentFilter_ShouldReturn409_WhenLockAcquireFails()
    {
        // Arrange
        var attr = new IdempotentAttribute(5);
        var filter = new IdempotentFilter(attr);

        var httpContext = new DefaultHttpContext();
        var lockProvider = Substitute.For<IDistributedLockProvider>();
        
        // 模拟分布式锁获取失败 (返回 null)
        lockProvider.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDistributedLockHandle?>(null));

        var services = new ServiceCollection();
        services.AddSingleton(lockProvider);
        httpContext.RequestServices = services.BuildServiceProvider();

        var invocationContext = Substitute.For<EndpointFilterInvocationContext>();
        invocationContext.HttpContext.Returns(httpContext);

        EndpointFilterDelegate next = (ctx) => ValueTask.FromResult<object?>("success");

        // Act
        var result = await filter.InvokeAsync(invocationContext, next);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse>(result);
        Assert.Equal(StatusCodes.Status409Conflict, apiResponse.Code);
    }
}
