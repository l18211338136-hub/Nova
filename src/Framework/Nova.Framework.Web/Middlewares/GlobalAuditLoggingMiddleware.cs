using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Constants;
using Nova.Contracts.CQRS;
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
        // 排除 Scalar / OpenAPI / 静态资源 / 审计日志查询请求 / 物理文件与图片预览流传输请求
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/v1/audit", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/identity/auth-audit-logs", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/nova-storage", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/storage/files/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("favicon") ||
            // SSE 长连接（如 MCP 网关 /api/mcp/sse）必须透传响应流：
            // 本中间件会把 Response.Body 换成 MemoryStream，等 _next 结束才回拷真实流，
            // 而 SSE 的 _next 在连接断开前永不结束，缓冲会导致 endpoint/message 事件永远到不了客户端
            context.Request.Headers.Accept.ToString()
                .Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var userId = GetUserId(context);
        var tenantId = GetTenantId(context);
        var clientIp = GetClientIp(context);
        var httpMethod = context.Request.Method;

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

            try
            {
                responsePayload = await ReadResponseBodyAsync(context.Response);
                if (originalBodyStream.CanWrite)
                {
                    await responseBodyMemoryStream.CopyToAsync(originalBodyStream);
                }
            }
            catch
            {
                // 忽略二次复制异常，保障原始异常能正常向上抛出
            }
            throw; // 向上抛出给 GlobalExceptionMiddleware
        }
        finally
        {
            context.Response.Body = originalBodyStream;

            try
            {
                var endpoint = context.GetEndpoint();
                var actionName = endpoint?.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary
                    ?? endpoint?.Metadata.GetMetadata<ApiEndpointAttribute>()?.Summary
                    ?? context.Request.RouteValues["action"]?.ToString() 
                    ?? context.Request.RouteValues["controller"]?.ToString();

                var statusCode = !isSuccess 
                    ? (context.Response.StatusCode >= 400 ? context.Response.StatusCode : 500) 
                    : (context.Response.StatusCode > 0 ? context.Response.StatusCode : 200);

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

    private static string GetTenantId(HttpContext context)
    {
        // 1. 从已认证的 JWT User Claims 中优先获取 (真实身份保障)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claimTenant = context.User.FindFirst("tenantId")?.Value 
                ?? context.User.FindFirst("tenant")?.Value 
                ?? context.User.FindFirst(ClaimTypes.GroupSid)?.Value;

            if (!string.IsNullOrWhiteSpace(claimTenant))
            {
                return claimTenant;
            }
        }

        // 2. 从 HTTP Header 中获取 (适应未认证接口如 Login/Register)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader))
        {
            return tenantHeader.ToString();
        }
        if (context.Request.Headers.TryGetValue("Tenant", out var tHeader) && !string.IsNullOrWhiteSpace(tHeader))
        {
            return tHeader.ToString();
        }

        // 3. 从 Finbuckle / HttpContext 中解构当前租户信息
        foreach (var item in context.Items.Values)
        {
            if (item != null && item.GetType().Name.Contains("TenantContext"))
            {
                var tenantInfoProp = item.GetType().GetProperty("TenantInfo");
                var tenantInfo = tenantInfoProp?.GetValue(item);
                if (tenantInfo != null)
                {
                    var idProp = tenantInfo.GetType().GetProperty("Identifier") ?? tenantInfo.GetType().GetProperty("Id");
                    var identifier = idProp?.GetValue(tenantInfo)?.ToString();
                    if (!string.IsNullOrWhiteSpace(identifier)) return identifier;
                }
            }
        }

        // 4. 若全流程未显式传递特定子租户（如匿名/获取验证码/宿主管理），属于系统宿主 root 租户
        return TenantConstants.RootTenantId;
    }

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(rawIp, out _))
            {
                return rawIp;
            }
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private static bool IsTextOrJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return true;
        var lower = contentType.ToLowerInvariant();
        return lower.Contains("json") ||
               lower.Contains("text") ||
               lower.Contains("xml") ||
               lower.Contains("html") ||
               lower.Contains("form-urlencoded");
    }

    private static string? SanitizeNullBytes(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Contains('\0') ? input.Replace("\0", string.Empty) : input;
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        if (!context.Request.ContentLength.HasValue || context.Request.ContentLength == 0) return null;
        if (context.Request.HasFormContentType) return "[Form Data]";

        if (!IsTextOrJsonContentType(context.Request.ContentType))
        {
            return "[Binary Data]";
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        body = SanitizeNullBytes(body);

        if (body != null && body.Length > 65536)
        {
            body = body.Substring(0, 65536) + " [Truncated...]";
        }

        return body;
    }

    private static async Task<string?> ReadResponseBodyAsync(HttpResponse response)
    {
        if (response.Body.CanSeek)
        {
            if (!IsTextOrJsonContentType(response.ContentType))
            {
                return "[Binary Data]";
            }

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

            text = SanitizeNullBytes(text);

            if (text != null && text.Length > 65536) return text.Substring(0, 65536) + " [Truncated...]";
            return text;
        }
        return null;
    }
}
