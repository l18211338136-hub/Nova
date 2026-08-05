using Nova.Framework.Web.Logging;
using Nova.Modules.Audit.Domain.OperationLogs;
using Nova.Modules.Audit.Domain.Services;
using Xunit;

namespace Nova.UnitTests;

public class OperationLogDomainTests
{
    private readonly ISanitizerEngine _sanitizer = new DefaultSanitizerEngine();

    [Fact]
    public void Create_ShouldInitializeOperationLogWithInProgressStatus()
    {
        var traceId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var clientIp = "192.168.1.100";
        var httpMethod = "POST";
        var requestPath = "/api/v1/agents";

        var log = OperationLog.Create(traceId, userId, clientIp, httpMethod, requestPath);

        Assert.NotNull(log);
        Assert.Equal(traceId, log.TraceId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(clientIp, log.ClientIp);
        Assert.Equal(httpMethod, log.HttpMethod);
        Assert.Equal(requestPath, log.RequestPath);
        Assert.Equal(ExecutionStatus.InProgress, log.Status);
        Assert.False(log.HasSanitizedData ?? false);
        Assert.Empty(log.SanitizationDetails);
    }

    [Fact]
    public void Create_WithAllNullFields_ShouldSucceed()
    {
        var log = OperationLog.Create(null, null, null, null, null, null);

        Assert.NotNull(log);
        Assert.Null(log.TraceId);
        Assert.Null(log.UserId);
        Assert.Null(log.ClientIp);
        Assert.Null(log.HttpMethod);
        Assert.Null(log.RequestPath);
        Assert.Null(log.ActionName);
        Assert.Null(log.RequestPayload);
        Assert.Null(log.ResponsePayload);
        Assert.Null(log.StatusCode);
        Assert.Null(log.ElapsedMs);
        Assert.Equal(ExecutionStatus.InProgress, log.Status);
        Assert.Null(log.IsSlowRequest);
        Assert.Null(log.HasSanitizedData);
    }

    [Fact]
    public void SetAndSanitizeRequestPayload_ShouldMaskSensitiveDataAndAddSanitizationDetail()
    {
        var log = OperationLog.Create("trace-123", Guid.NewGuid(), "127.0.0.1", "POST", "/api/v1/auth/login");
        var rawJson = "{\"username\": \"admin\", \"password\": \"SuperSecret123!\"}";

        log.SetAndSanitizeRequestPayload(rawJson, _sanitizer);

        Assert.True(log.HasSanitizedData);
        Assert.DoesNotContain("SuperSecret123!", log.RequestPayload);
        Assert.Contains("***SENSITIVE***", log.RequestPayload);
        Assert.Single(log.SanitizationDetails);
        Assert.Equal("password", log.SanitizationDetails.First().FieldName);
    }

    [Fact]
    public void MarkAsSuccess_ShouldCalculateSlowRequestThresholdCorrectly()
    {
        var log = OperationLog.Create("trace-456", null, "127.0.0.1", "GET", "/api/v1/users");

        log.MarkAsSuccess(200, 600, slowThresholdMs: 500);

        Assert.Equal(ExecutionStatus.Success, log.Status);
        Assert.Equal(200, log.StatusCode);
        Assert.Equal(600, log.ElapsedMs);
        Assert.True(log.IsSlowRequest);
    }

    [Fact]
    public void MarkAsFailed_ShouldCaptureExceptionDetails()
    {
        var log = OperationLog.Create("trace-789", null, "127.0.0.1", "POST", "/api/v1/checkout");
        var exception = new InvalidOperationException("Insufficient funds");

        log.MarkAsFailed(exception, 400);

        Assert.Equal(ExecutionStatus.Failed, log.Status);
        Assert.Equal(400, log.StatusCode);
        Assert.Equal("Insufficient funds", log.ErrorMessage);
    }

    [Fact]
    public void DynamicSensitiveRules_ShouldMaskCustomKey()
    {
        var provider = new DefaultSensitiveRuleProvider(new[] { "customSecretKey", "ssn" });
        var engine = new DefaultSanitizerEngine(provider);

        var log = OperationLog.Create("trace-custom", null, "127.0.0.1", "POST", "/api/v1/custom");
        var rawJson = "{\"username\": \"john\", \"customSecretKey\": \"my-super-secret-value\", \"ssn\": \"123-45-6789\"}";

        log.SetAndSanitizeRequestPayload(rawJson, engine);

        Assert.True(log.HasSanitizedData);
        Assert.DoesNotContain("my-super-secret-value", log.RequestPayload);
        Assert.DoesNotContain("123-45-6789", log.RequestPayload);
        Assert.Contains("***SENSITIVE***", log.RequestPayload);
        Assert.Equal(2, log.SanitizationDetails.Count);
    }
}
