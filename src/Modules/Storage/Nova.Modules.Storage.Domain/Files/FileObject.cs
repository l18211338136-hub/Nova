using Nova.Contracts.Storage;
using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Storage.Domain.Files;

public class FileObject : FullAuditedEntity<Guid>
{
    public string FileName { get; private set; } = default!;
    public string FileKey { get; private set; } = default!;
    public string BucketName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSize { get; private set; }
    public string? FileHash { get; private set; }
    public StorageProviderType Provider { get; private set; }
    public StorageAccessMode AccessMode { get; private set; }

    /// <summary>
    /// 相对访问路径（不包含绝对域名，方便后期更换 CDN 或 S3 域名时无需迁移数据库）
    /// </summary>
    public string? AccessUrl { get; private set; }

    private FileObject() { }

    public static FileObject Create(
        string fileName,
        string fileKey,
        string bucketName,
        string contentType,
        long fileSize,
        StorageProviderType provider,
        StorageAccessMode accessMode = StorageAccessMode.Private,
        string? fileHash = null,
        string? accessUrl = null)
    {
        return new FileObject
        {
            Id = Guid.CreateVersion7(),
            FileName = fileName,
            FileKey = fileKey,
            BucketName = bucketName,
            ContentType = contentType,
            FileSize = fileSize,
            Provider = provider,
            AccessMode = accessMode,
            FileHash = fileHash,
            AccessUrl = accessUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateAccessUrl(string accessUrl)
    {
        AccessUrl = accessUrl;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
