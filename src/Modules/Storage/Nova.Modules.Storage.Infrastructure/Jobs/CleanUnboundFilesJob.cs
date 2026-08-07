using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Storage;
using Nova.Modules.Storage.Application.Database;

namespace Nova.Modules.Storage.Infrastructure.Jobs;

public class CleanUnboundFilesJob
{
    private readonly IStorageDbContext _db;
    private readonly IStorageProvider _storageProvider;
    private readonly ILogger<CleanUnboundFilesJob> _logger;

    public CleanUnboundFilesJob(IStorageDbContext db, IStorageProvider storageProvider, ILogger<CleanUnboundFilesJob> logger)
    {
        _db = db;
        _storageProvider = storageProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Storage.GC] 启动未绑定文件自动清理回收扫描...");

        var threshold = DateTimeOffset.UtcNow.AddHours(-24);

        // 1. 查找创建超过 24 小时未绑定任何 Attachments 的物理文件 ID
        var unboundFileIds = await _db.FileObjects
            .Where(f => f.CreatedAt <= threshold)
            .Where(f => !_db.Attachments.Any(a => a.FileId == f.Id))
            .Select(f => f.Id)
            .Take(100) // 每次 Batch 处理 100 个
            .ToListAsync(cancellationToken);

        if (unboundFileIds.Count == 0)
        {
            _logger.LogInformation("[Storage.GC] 未扫描到需要清理的未绑定文件。");
            return;
        }

        var unboundFiles = await _db.FileObjects
            .Where(f => unboundFileIds.Contains(f.Id))
            .ToListAsync(cancellationToken);

        int deletedCount = 0;
        foreach (var file in unboundFiles)
        {
            // 校验是否有相同 FileHash 且受引用的其他 FileObject 共享同路径物理文件
            var samePathSharedCount = await _db.FileObjects
                .AsNoTracking()
                .CountAsync(x => x.FileKey == file.FileKey && x.Id != file.Id, cancellationToken);

            if (samePathSharedCount == 0)
            {
                // 无其他记录共享同路径，可以安全物理删除
                await _storageProvider.DeleteAsync(file.FileKey, file.BucketName, cancellationToken);
            }

            _db.FileObjects.Remove(file);
            deletedCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[Storage.GC] 成功清理了 {Count} 个过期未绑定文件及其存储资源。", deletedCount);
    }
}
