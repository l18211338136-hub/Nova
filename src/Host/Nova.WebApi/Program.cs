using Nova.Framework.Infrastructure.Extensions;
using Nova.Framework.Persistence.Extensions;
using Nova.Framework.Jobs;
using Nova.Framework.MultiTenancy;
using Nova.Framework.EventBus.Outbox;
using Nova.Framework.Web.Authentication;
using Nova.Framework.Web.Cors;
using Nova.Framework.Web.CQRS;
using Nova.Framework.Web.Extensions;
using Nova.Framework.Web.Middlewares;
using Nova.Framework.Web.Modular;
using Nova.Framework.Web.OpenApi;
using Nova.Framework.Web.Security;
using Nova.WebApi.Extensions;
using Scalar.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNovaCaching(builder.Configuration);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<JwtBearerDocumentTransformer>();
    options.AddOperationTransformer<JwtBearerOperationTransformer>();
    options.AddOperationTransformer<TenantHeaderOperationTransformer>();
    options.AddOperationTransformer<AutoEndpointOperationTransformer>();
    options.AddSchemaTransformer<ServerInjectedPropertySchemaTransformer>();
});



builder.Services.AddNovaCors(builder.Configuration);
builder.Services.AddNovaJwtAuthentication(builder.Configuration);
builder.Services.AddNovaOData();
builder.Services.AddModules(builder.Configuration);
builder.Services.AddNovaMultiTenancy(builder.Configuration);
builder.Services.AddNovaOutbox(builder.Configuration);
builder.Services.AddNovaJobs(builder.Configuration);
builder.Services.AddNovaHealthChecks(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseNovaCors();
app.UseNovaLocalStorage();

// MCP 导入：Swagger 文档可能很大（企业级 API 常超 30MB 限制）。
// 因为 PayloadEncryptionMiddleware 会提前读取 Body，所以必须在更早的阶段针对特定路由放宽限制。
app.UseEndpointRequestBodySizeLimit("/api/mcp/parse", 100 * 1024 * 1024);

// 必须放在此处，确保所有的 Payload（请求/响应）被加解密后再交给下游中间件处理
app.UseNovaPayloadEncryption();

// 多租户与 JWT 认证必须在全局审计日志中间件之前执行，确保 HttpContext 中能够正确提取已解析的 TenantInfo 和 User Claims
app.UseNovaMultiTenancy();
app.UseAuthorization();



app.UseMiddleware<GlobalAuditLoggingMiddleware>();

// 必须在 Hangfire (UseNovaJobs) 之前执行，否则 Hangfire 连不上不存在的库
await app.ApplyDatabaseMigrationsAsync();

app.UseNovaJobs(requireAuth: app.Configuration.GetValue<bool>("NovaJobs:RequireAuthorization"));

app.UseNovaHealthChecks();
app.MapModuleEndpoints();
app.MapPayloadEncryptionEndpoints();

app.Run();
