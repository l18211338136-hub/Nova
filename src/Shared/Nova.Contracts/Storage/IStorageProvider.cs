namespace Nova.Contracts.Storage;

public interface IStorageProvider
{
    StorageProviderType ProviderType { get; }

    /// <summary>
    /// 上传文件到物理存储 (S3/MinIO/Local)
    /// </summary>
    Task<StorageUploadResponse> UploadAsync(StorageUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取物理文件流
    /// </summary>
    Task<Stream?> DownloadAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 物理删除文件
    /// </summary>
    Task<bool> DeleteAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查物理文件是否存在
    /// </summary>
    Task<bool> ExistsAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成单个文件的预签名直传/下载 URL
    /// </summary>
    Task<string> GetPreSignedUrlAsync(string fileKey, TimeSpan expiresIn, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量生成多个文件的预签名直传/上传 URL 数组
    /// </summary>
    Task<List<PreSignedUrlResponseItem>> GetPreSignedUploadUrlsAsync(List<PreSignedUrlRequestItem> items, TimeSpan expiresIn, string? bucketName = null, CancellationToken cancellationToken = default);
}
