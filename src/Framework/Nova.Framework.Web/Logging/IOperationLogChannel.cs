namespace Nova.Framework.Web.Logging;

public record MaskedFieldInfo(string FieldName, string MaskedRule);
public record SanitizerResult(string SanitizedText, IReadOnlyList<MaskedFieldInfo> MaskedFields);

public interface ISanitizerEngine
{
    SanitizerResult Sanitize(string inputJsonOrText);
}

public record OperationLogRequest(
    string TraceId,
    Guid? UserId,
    string ClientIp,
    string HttpMethod,
    string RequestPath,
    string? ActionName,
    string? RequestPayload,
    string? ResponsePayload,
    int StatusCode,
    long ElapsedMs,
    bool IsSuccess,
    string? ErrorMessage,
    string? ExceptionStackTrace
);

public interface IOperationLogChannel
{
    ValueTask WriteAsync(OperationLogRequest request, string? tenantId, CancellationToken cancellationToken = default);
}
