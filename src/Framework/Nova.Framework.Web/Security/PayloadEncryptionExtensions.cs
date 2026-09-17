using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Responses;
using Nova.Framework.Web.Security.Cryptography;

namespace Nova.Framework.Web.Security;

public record PublicKeyDto(string PublicKey, bool IsEnabled = true);

public static class PayloadEncryptionExtensions
{
    /// <summary>
    /// 注册 Payload 加解密中间件
    /// </summary>
    public static IApplicationBuilder UseNovaPayloadEncryption(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        
        // 在开发环境下，如果发现密钥是占位符或为空，则全自动生成并回写到 json 中
        if (env.EnvironmentName == "Development")
        {
            var privKey = configuration["Encryption:RsaPrivateKey"];
            if (string.IsNullOrEmpty(privKey) || privKey.Contains("请替换"))
            {
                var (publicKey, privateKey) = RsaHelper.GenerateKeyPair();
                
                var configPath = System.IO.Path.Combine(env.ContentRootPath, "appsettings.Development.json");
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    
                    // 用正则表达式替换现有的占位符或空值
                    var safePriv = privateKey.Replace("\r", "").Replace("\n", "\\n");
                    var safePub = publicKey.Replace("\r", "").Replace("\n", "\\n");
                    
                    json = System.Text.RegularExpressions.Regex.Replace(json, 
                        @"""RsaPrivateKey""\s*:\s*"".*?""", 
                        $"\"RsaPrivateKey\": \"{safePriv}\"");
                        
                    json = System.Text.RegularExpressions.Regex.Replace(json, 
                        @"""RsaPublicKey""\s*:\s*"".*?""", 
                        $"\"RsaPublicKey\": \"{safePub}\"");
                        
                    System.IO.File.WriteAllText(configPath, json);
                    
                    // 热重载配置
                    if (configuration is IConfigurationRoot root)
                    {
                        root.Reload();
                    }
                }
            }
        }

        return app.UseMiddleware<PayloadEncryptionMiddleware>();
    }

    /// <summary>
    /// 注册获取 RSA 公钥的端点
    /// </summary>
    public static IEndpointRouteBuilder MapPayloadEncryptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/security/public-key", (IConfiguration configuration) =>
        {
            var isEnabled = configuration.GetValue<bool>("Encryption:Enabled", true);
            var publicKey = configuration["Encryption:RsaPublicKey"];
            if (isEnabled && string.IsNullOrEmpty(publicKey))
            {
                return Results.NotFound(ApiResponse.Error("RSA keys are not configured."));
            }
            return Results.Ok(ApiResponse<PublicKeyDto>.Success(new PublicKeyDto(publicKey ?? "", isEnabled)));
        })
        .WithTags("Security")
        .WithName("GetPublicKey")
        .WithSummary("获取公钥")
        .Produces<ApiResponse<PublicKeyDto>>();

        return endpoints;
    }
}
