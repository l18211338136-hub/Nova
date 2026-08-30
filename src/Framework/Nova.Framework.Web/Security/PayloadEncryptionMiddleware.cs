using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nova.Framework.Web.Security.Cryptography;

namespace Nova.Framework.Web.Security;

public class PayloadEncryptionMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<PayloadEncryptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<PayloadEncryptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var privateKeyPem = _configuration["Encryption:RsaPrivateKey"];
        
        // 如果未配置私钥，则跳过加密中间件
        if (string.IsNullOrEmpty(privateKeyPem))
        {
            await _next(context);
            return;
        }

        // 尝试从 Header 获取前端用 RSA 公钥加密后的 AES 密钥
        if (!context.Request.Headers.TryGetValue("X-Encryption-Key", out var encryptedAesKeyBase64))
        {
            // 对于不带密钥的请求（如普通的 GET 请求、Swagger UI 等），直接放行
            await _next(context);
            return;
        }

        byte[] aesKey;
        try
        {
            var encryptedAesKeyBytes = Convert.FromBase64String(encryptedAesKeyBase64.ToString());
            aesKey = RsaHelper.Decrypt(encryptedAesKeyBytes, privateKeyPem);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt AES key from X-Encryption-Key header.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid encryption key.");
            return;
        }

        // 1. 拦截并解密 Request Body (仅对 POST/PUT/PATCH 生效)
        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(memoryStream);
                var encryptedBody = memoryStream.ToArray();
                
                // 前端通常把加密后的内容作为 Base64 纯文本发送
                var base64String = Encoding.UTF8.GetString(encryptedBody).Trim('"'); 
                var encryptedBytes = Convert.FromBase64String(base64String);
                
                var decryptedBytes = AesGcmHelper.Decrypt(encryptedBytes, aesKey);
                
                // 替换原有的 Request Body 流
                var decryptedStream = new MemoryStream(decryptedBytes);
                context.Request.Body = decryptedStream;
                context.Request.ContentLength = decryptedBytes.Length;
                
                // 强制将 ContentType 改回 JSON，以确保后续的 ModelBinder 正常工作
                context.Request.ContentType = "application/json; charset=utf-8";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt request payload.");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Failed to decrypt payload.");
                return;
            }
        }

        // 2. 拦截 Response Body
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        try
        {
            // 执行下游所有的 Controller 和中间件业务逻辑
            await _next(context);
        }
        finally
        {
            // 3. 恢复 Response Body (即使出现异常，也保证流能被正确复原，防止外层读写已释放的内存流)
            context.Response.Body = originalResponseBodyStream;
        }

        // 仅对 JSON 或纯文本响应进行加密，防止破坏文件下载流 (如 application/octet-stream, pdf, excel 等)
        var isJsonResponse = context.Response.ContentType?.Contains("application/json") == true || 
                             context.Response.ContentType?.Contains("text/plain") == true;

        if (responseBodyMemoryStream.Length > 0 && context.Response.StatusCode is >= 200 and < 300 && isJsonResponse)
        {
            var responseBytes = responseBodyMemoryStream.ToArray();
            
            try
            {
                var encryptedResponseBytes = AesGcmHelper.Encrypt(responseBytes, aesKey);
                var base64EncryptedResponse = Convert.ToBase64String(encryptedResponseBytes);
                var finalResponseBytes = Encoding.UTF8.GetBytes(base64EncryptedResponse);

                // 返回纯文本格式的 Base64 密文
                context.Response.ContentType = "text/plain";
                context.Response.ContentLength = finalResponseBytes.Length;
                await context.Response.Body.WriteAsync(finalResponseBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt response payload.");
                throw;
            }
        }
        else
        {
            // 如果是异常响应或无内容的响应，直接原样返回
            var bytes = responseBodyMemoryStream.ToArray();
            await context.Response.Body.WriteAsync(bytes);
        }
    }
}
