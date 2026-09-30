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

// 仅开发环境启用 HTTPS 重定向（本地有开发证书）。
// 生产/裸 VM 只监听 http 时若开启，浏览器 http 访问会被 307 重定向到无监听的 https 端口，
// 导致 /scalar/v1 文档与接口都无法打开。正式上线若前置 nginx 等 TLS 终止代理，重定向应在代理层处理。
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseNovaCors();
app.UseNovaLocalStorage();

// 部署前端 SPA（admin 构建产物放入 wwwroot）：
// UseFileServer = UseDefaultFiles + UseStaticFiles，使 / 默认可命中 index.html，
// 并直接服务 /assets/* 等静态资源。
app.UseFileServer();

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

// SPA 回退：所有未被 API / 文档 / 静态资源命中的 GET 请求都回退到 index.html，
// 交给前端（TanStack Router 等）做客户端路由。必须放在所有 API 端点映射之后。
// 防御式：仅当 wwwroot/index.html 存在时才注册，避免前端尚未部署时后端启动崩溃。
if (app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
{
    app.MapFallbackToFile("index.html");
}

app.Run();
