using Microsoft.AspNetCore.Http;
using NSubstitute;
using Nova.Contracts.RateLimiting;
using Nova.Framework.Web.RateLimiting;

namespace Nova.UnitTests;

public class DistributedRateLimitingTests
{
    [Fact]
    public async Task DistributedRateLimitingFilter_ShouldPassThrough_WhenRedisNotAvailable()
    {
        // Arrange
        var attr = new DistributedRateLimitAttribute(permitLimit: 5, windowSeconds: 60);
        var filter = new DistributedRateLimitingFilter(attr);

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
}
