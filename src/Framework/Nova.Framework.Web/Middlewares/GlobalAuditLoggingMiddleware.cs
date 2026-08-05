using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nova.Framework.Web.Logging;

namespace Nova.Framework.Web.Middlewares;

public class GlobalAuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalAuditLoggingMiddleware> _logger;

    public GlobalAuditLoggingMiddleware(RequestDelegate next, ILogger<GlobalAuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOperationLogChannel logChannel, ISanitizerEngine sanitizer)
    {
        // 排除 Scalar / OpenAPI / 静态资源 / 审计日志查询请求
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/v1/audit", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/identity/auth-audit-logs", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("favicon"))
        {
            await _next(context);
            return;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var userId = GetUserId(context);
        var tenantId = GetTenantId(context);
        var clientIp = GetClientIp(context);
        var httpMethod = context.Request.Method;
        var actionName = context.Request.RouteValues["action"]?.ToString() 
            ?? context.Request.RouteValues["controller"]?.ToString();

        string? requestPayload = await ReadRequestBodyAsync(context);
        string? responsePayload = null;

        var originalBodyStream = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = true;
        string? errorMessage = null;
        string? exceptionStackTrace = null;

        try
        {
            await _next(context);
            stopwatch.Stop();

            responsePayload = await ReadResponseBodyAsync(context.Response);
            await responseBodyMemoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            isSuccess = false;
            errorMessage = ex.Message;
            exceptionStackTrace = ex.StackTrace;

            responsePayload = await ReadResponseBodyAsync(context.Response);
            await responseBodyMemoryStream.CopyToAsync(originalBodyStream);
            throw; // 向上抛出给 GlobalExceptionMiddleware
        }
        finally
        {
            context.Response.Body = originalBodyStream;

            try
            {
                var statusCode = context.Response.StatusCode > 0 ? context.Response.StatusCode : (isSuccess ? 200 : 500);

                var logRequest = new OperationLogRequest(
                    TraceId: traceId,
                    UserId: userId,
                    ClientIp: clientIp,
                    HttpMethod: httpMethod,
                    RequestPath: path,
                    ActionName: actionName,
                    RequestPayload: requestPayload,
                    ResponsePayload: responsePayload,
                    StatusCode: statusCode,
                    ElapsedMs: stopwatch.ElapsedMilliseconds,
                    IsSuccess: isSuccess,
                    ErrorMessage: errorMessage,
                    ExceptionStackTrace: exceptionStackTrace
                );

                await logChannel.WriteAsync(logRequest, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuditMiddleware] Failed to push OperationLog into channel.");
            }
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var nameId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(nameId, out var userId)) return userId;
        }
        return null;
    }

    private static string? GetTenantId(HttpContext context)
    {
        // 1. 从 HTTP Header 中获取
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader))
        {
            return tenantHeader.ToString();
        }
        if (context.Request.Headers.TryGetValue("Tenant", out var tHeader) && !string.IsNullOrWhiteSpace(tHeader))
        {
            return tHeader.ToString();
        }

        // 2. 从 JWT User Claims 中获取
        var claimTenant = context.User.FindFirst("tenantId")?.Value 
            ?? context.User.FindFirst("tenant")?.Value 
            ?? context.User.FindFirst(ClaimTypes.GroupSid)?.Value;

        if (!string.IsNullOrWhiteSpace(claimTenant))
        {
            return claimTenant;
        }

        // 3. 从 HttpContext.Items 中尝试获取 Finbuckle 解析的 TenantInfo
        foreach (var item in context.Items.Values)
        {
            if (item != null && item.GetType().Name.Contains("TenantContext"))
            {
                var tenantInfoProp = item.GetType().GetProperty("TenantInfo");
                var tenantInfo = tenantInfoProp?.GetValue(item);
                if (tenantInfo != null)
                {
                    var idProp = tenantInfo.GetType().GetProperty("Identifier");
                    var identifier = idProp?.GetValue(tenantInfo)?.ToString();
                    if (!string.IsNullOrWhiteSpace(identifier)) return identifier;
                }
            }
        }

        // 4. 若全流程无法获取租户，返回 null
        return null;
    }

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        if (!context.Request.ContentLength.HasValue || context.Request.ContentLength == 0) return null;
        if (context.Request.HasFormContentType) return "[Form Data]";

        context.Request.EnableBuffering();
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (body.Length > 65536)
        {
            body = body.Substring(0, 65536) + " [Truncated...]";
        }

        return body;
    }

    private static async Task<string?> ReadResponseBodyAsync(HttpResponse response)
    {
        if (response.Body.CanSeek)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(
                response.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var text = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            if (string.IsNullOrWhiteSpace(text)) return null;
            if (text.Length > 65536) return text.Substring(0, 65536) + " [Truncated...]";
            return text;
        }
        return null;
    }
}
