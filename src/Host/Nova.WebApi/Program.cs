using Scalar.AspNetCore;
using Nova.Framework.Web.OpenApi;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Modular;
using Nova.Framework.Web.Authentication;
using Nova.Framework.Web.Cors;
using Nova.Framework.Web.Extensions;
using Nova.WebApi.Extensions;
using Nova.Framework.Infrastructure.Extensions;
using Nova.Framework.Web.CQRS;
using Nova.Framework.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNovaCaching(builder.Configuration);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<JwtBearerDocumentTransformer>();
    options.AddOperationTransformer<JwtBearerOperationTransformer>();
    options.AddOperationTransformer<TenantHeaderOperationTransformer>();
    options.AddOperationTransformer<AutoEndpointOperationTransformer>();
});

builder.Services.AddNovaCors(builder.Configuration);
builder.Services.AddNovaJwtAuthentication(builder.Configuration);
builder.Services.AddNovaOData();
builder.Services.AddModules(builder.Configuration);
builder.Services.AddNovaMultiTenancy(builder.Configuration);
builder.Services.AddNovaJobs(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<Nova.Framework.Web.Middlewares.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapModuleEndpoints();
}

app.UseHttpsRedirection();
app.UseNovaCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseNovaJobs(requireAuth: false); // 开发阶段暂不限制，生产环境改为 true
app.UseNovaMultiTenancy();

await app.ApplyDatabaseMigrationsAsync();

app.Run();


