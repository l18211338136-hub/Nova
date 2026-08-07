using Finbuckle.MultiTenant.Abstractions;
using Mapster;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nova.Contracts.Security;
using Nova.Contracts.Storage;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Persistence.Interceptors;
using Nova.Framework.Web.Modular;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Database;
using Nova.Modules.Storage.Domain.Files;
using Nova.Modules.Storage.Infrastructure.Jobs;
using Nova.Modules.Storage.Infrastructure.Persistence;
using Nova.Modules.Storage.Infrastructure.Providers;

namespace Nova.Modules.Storage.Api;

public class UploadFileFormRequest
{
    public IFormFile File { get; set; } = default!;
}

public class StorageModule : IModule
{
    public string Name => "Storage";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        // 根据配置中 ActiveProvider 的配置动态选择注入具体的物理存储实现 (S3/MinIO 或 本地 Local)
        services.AddScoped<IStorageProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            if (options.ActiveProvider.Equals("Local", StringComparison.OrdinalIgnoreCase) ||
                options.ActiveProvider.Equals("LocalStorage", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<LocalStorageProvider>();
            }

            return sp.GetRequiredService<S3StorageProvider>();
        });

        services.AddScoped<S3StorageProvider>();
        services.AddScoped<LocalStorageProvider>();
        services.AddTransient<CleanUnboundFilesJob>();

        // 注册 StorageDbContext 并开启自动审计拦截与 Finbuckle 自动多租户隔离
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<StorageDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IStorageDbContext>(sp => sp.GetRequiredService<StorageDbContext>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // 挂载 CleanUnboundFilesJob 每日零点自动运行未绑定文件清理回收 GC
        try
        {
            RecurringJob.AddOrUpdate<CleanUnboundFilesJob>(
                "clean-unbound-storage-files",
                job => job.ExecuteAsync(default),
                Cron.Daily
            );
        }
        catch { }

        // 挂载通用物理文件表单/流上传 Endpoint
        endpoints.MapPost("/api/v1/storage/upload", async (
            [FromForm] UploadFileFormRequest request,
            IStorageProvider storageProvider,
            IStorageDbContext db,
            ICurrentUser currentUser) =>
        {
            var file = request.File;
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<FileObject>.Error("未接收到上传的文件"));
            }

            using var stream = file.OpenReadStream();
            var uploadReq = new StorageUploadRequest
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileStream = stream
            };

            var res = await storageProvider.UploadAsync(uploadReq);

            var fileObj = FileObject.Create(
                file.FileName,
                res.FileKey,
                res.BucketName,
                file.ContentType,
                res.FileSize,
                StorageProviderType.LocalStorage,
                accessUrl: res.AccessUrl
            );

            db.FileObjects.Add(fileObj);
            await db.SaveChangesAsync();

            return Results.Ok(ApiResponse<StorageFileDto>.Success(fileObj.Adapt<StorageFileDto>()));
        })
        .DisableAntiforgery()
        .WithTags("Storage")
        .WithSummary("物理文件表单/流上传 Endpoint")
        .Accepts<UploadFileFormRequest>("multipart/form-data")
        .Produces<ApiResponse<StorageFileDto>>()
        .RequireAuthorization();

        // 挂载根据文件 ID 获取二进制流 Endpoint（如公开/私有头像、文件展示）
        endpoints.MapGet("/api/v1/storage/files/{id:guid}", async (
            Guid id,
            IStorageDbContext db,
            IStorageProvider storageProvider,
            CancellationToken cancellationToken) =>
        {
            var fileObj = await db.FileObjects.FindAsync(new object[] { id }, cancellationToken);
            if (fileObj == null)
            {
                return Results.NotFound("文件不存在");
            }

            var stream = await storageProvider.DownloadAsync(fileObj.FileKey, fileObj.BucketName, cancellationToken);
            if (stream == null)
            {
                return Results.NotFound("存储服务中找不到该文件");
            }

            return Results.File(stream, fileObj.ContentType ?? "application/octet-stream");
        })
        .AllowAnonymous()
        .WithTags("Storage")
        .WithSummary("根据文件ID获取二进制文件流");

        // 挂载根据相对路径 /nova-storage/{*fileKey} 获取文件流 Endpoint (透传 MinIO)
        endpoints.MapGet("/nova-storage/{*fileKey}", async (
            string fileKey,
            IStorageProvider storageProvider,
            CancellationToken cancellationToken) =>
        {
            var stream = await storageProvider.DownloadAsync(fileKey, null, cancellationToken);
            if (stream == null)
            {
                return Results.NotFound("文件不存在");
            }

            var contentType = fileKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                : fileKey.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileKey.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                : fileKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
                : fileKey.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif"
                : "application/octet-stream";

            return Results.File(stream, contentType);
        })
        .AllowAnonymous()
        .WithTags("Storage")
        .WithSummary("根据 Path 相对路径获取文件流 (MinIO/Local)");
    }
}
