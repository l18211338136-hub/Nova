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
            RecurringJob.RemoveIfExists("clean-orphan-storage-files");

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
        .WithName("UploadStorageFile")
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
        .WithName("GetStorageFileContent")
        .WithSummary("根据文件ID获取二进制文件流");

        // 挂载根据 Path 相对路径获取文件流 Endpoint (支持 S3 / Local 混合自动降级查寻)
        endpoints.MapGet("/nova-storage/{*fileKey}", async (
            string fileKey,
            IStorageProvider storageProvider,
            LocalStorageProvider localStorageProvider,
            S3StorageProvider s3StorageProvider,
            CancellationToken cancellationToken) =>
        {
            var stream = await storageProvider.DownloadAsync(fileKey, null, cancellationToken);
            if (stream == null)
            {
                stream = await localStorageProvider.DownloadAsync(fileKey, null, cancellationToken)
                    ?? await s3StorageProvider.DownloadAsync(fileKey, null, cancellationToken);
            }

            if (stream == null)
            {
                return Results.NotFound("文件不存在");
            }

            byte[] fileBytes;
            using (stream)
            {
                if (stream is MemoryStream ms)
                {
                    fileBytes = ms.ToArray();
                }
                else
                {
                    using var tempMs = new MemoryStream();
                    await stream.CopyToAsync(tempMs, cancellationToken);
                    fileBytes = tempMs.ToArray();
                }
            }

            var contentType = fileKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                : fileKey.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileKey.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                : fileKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
                : fileKey.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif"
                : "application/octet-stream";

            return Results.File(fileBytes, contentType);
        })
        .AllowAnonymous()
        .WithTags("Storage")
        .WithName("GetStorageFileByPath")
        .WithSummary("根据 Path 相对路径获取文件流 (MinIO/Local)");

        // 1. 文件卡片列表/分页/分类查询 Endpoint (默认每页 12 项)
        endpoints.MapGet("/api/v1/storage/files", async (
            IStorageDbContext db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            CancellationToken cancellationToken = default) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 12 : pageSize;

            var query = db.FileObjects.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(f => f.FileName.ToLower().Contains(keyword) || f.FileKey.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var cat = category.Trim().ToLower();
                if (cat == "image")
                {
                    query = query.Where(f => f.ContentType.StartsWith("image/"));
                }
                else if (cat == "video")
                {
                    query = query.Where(f => f.ContentType.StartsWith("video/"));
                }
                else if (cat == "audio")
                {
                    query = query.Where(f => f.ContentType.StartsWith("audio/"));
                }
                else if (cat == "document")
                {
                    query = query.Where(f => f.ContentType.Contains("pdf") ||
                                            f.ContentType.Contains("word") ||
                                            f.ContentType.Contains("document") ||
                                            f.ContentType.Contains("text") ||
                                            f.ContentType.Contains("json") ||
                                            f.ContentType.Contains("sheet") ||
                                            f.ContentType.Contains("excel"));
                }
                else if (cat == "archive")
                {
                    query = query.Where(f => f.ContentType.Contains("zip") ||
                                            f.ContentType.Contains("rar") ||
                                            f.ContentType.Contains("tar") ||
                                            f.ContentType.Contains("7z") ||
                                            f.ContentType.Contains("compressed"));
                }
            }

            var total = await query.LongCountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(f => f.Adapt<StorageFileDto>()).ToList();

            var result = new PagedResult<StorageFileDto>
            {
                Total = total,
                Items = dtos,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<StorageFileDto>>.Success(result));
        })
        .Produces<ApiResponse<PagedResult<StorageFileDto>>>()
        .RequireAuthorization()
        .WithTags("Storage")
        .WithName("GetStorageFiles")
        .WithSummary("分页/条件获取存储文件卡片列表 (默认每页12项)");

        // 覆盖上传更新物理文件内容 (同文件扩展名且访问链接保持不变)
        endpoints.MapPut("/api/v1/storage/files/{id:guid}/content", async (
            Guid id,
            [FromForm] UploadFileFormRequest request,
            IStorageDbContext db,
            IStorageProvider storageProvider,
            CancellationToken cancellationToken) =>
        {
            var fileObj = await db.FileObjects.FindAsync(new object[] { id }, cancellationToken);
            if (fileObj == null)
            {
                return Results.NotFound(ApiResponse<StorageFileDto>.Error("找不到指定文件"));
            }

            var newFile = request.File;
            if (newFile == null || newFile.Length == 0)
            {
                return Results.BadRequest(ApiResponse<StorageFileDto>.Error("未接收到覆盖的文件"));
            }

            // 严格校验文件后缀/扩展名一致
            var oldExt = Path.GetExtension(fileObj.FileName).ToLowerInvariant();
            var newExt = Path.GetExtension(newFile.FileName).ToLowerInvariant();

            if (!string.Equals(oldExt, newExt, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(ApiResponse<StorageFileDto>.Error($"替换文件的扩展名（{newExt}）必须与原文件扩展名（{oldExt}）保持一致！"));
            }

            using var stream = newFile.OpenReadStream();
            await storageProvider.OverwriteAsync(fileObj.FileKey, stream, newFile.ContentType, fileObj.BucketName, cancellationToken);

            fileObj.UpdateContent(newFile.Length, newFile.ContentType);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ApiResponse<StorageFileDto>.Success(fileObj.Adapt<StorageFileDto>()));
        })
        .DisableAntiforgery()
        .WithTags("Storage")
        .WithName("ReplaceStorageFileContent")
        .WithSummary("同扩展名覆盖上传更新物理文件内容 (保留原访问链接与 FileKey 不变)")
        .Accepts<UploadFileFormRequest>("multipart/form-data")
        .Produces<ApiResponse<StorageFileDto>>()
        .RequireAuthorization();

        // 2. 物理与记录一键删除 Endpoint
        endpoints.MapDelete("/api/v1/storage/files/{id:guid}", async (
            Guid id,
            IStorageDbContext db,
            IStorageProvider storageProvider,
            CancellationToken cancellationToken) =>
        {
            var fileObj = await db.FileObjects.FindAsync(new object[] { id }, cancellationToken);
            if (fileObj == null)
            {
                return Results.NotFound(ApiResponse<bool>.Error("找不到指定文件"));
            }

            try
            {
                await storageProvider.DeleteAsync(fileObj.FileKey, fileObj.BucketName, cancellationToken);
            }
            catch { }

            db.FileObjects.Remove(fileObj);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ApiResponse<bool>.Success(true));
        })
        .Produces<ApiResponse<bool>>()
        .RequireAuthorization()
        .WithTags("Storage")
        .WithName("DeleteStorageFile")
        .WithSummary("物理与数据库文件记录删除");

        // 3. 存储统计指标 Endpoint
        endpoints.MapGet("/api/v1/storage/stats", async (
            IStorageDbContext db,
            IOptions<StorageOptions> options,
            CancellationToken cancellationToken) =>
        {
            var totalFiles = await db.FileObjects.LongCountAsync(cancellationToken);
            var totalSize = await db.FileObjects.SumAsync(f => (long?)f.FileSize, cancellationToken) ?? 0;
            var activeProvider = options.Value.ActiveProvider ?? "Local";

            var stats = new
            {
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                ActiveProvider = activeProvider
            };

            return Results.Ok(ApiResponse<object>.Success(stats));
        })
        .Produces<ApiResponse<object>>()
        .RequireAuthorization()
        .WithTags("Storage")
        .WithName("GetStorageStats")
        .WithSummary("获取存储容量及提供商统计数据");
    }
}
