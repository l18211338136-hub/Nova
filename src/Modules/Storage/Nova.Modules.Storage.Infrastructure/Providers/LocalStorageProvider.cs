using Microsoft.Extensions.Options;
using Nova.Contracts.Storage;

namespace Nova.Modules.Storage.Infrastructure.Providers;

public class LocalStorageProvider : IStorageProvider
{
    private readonly StorageOptions _options;

    public StorageProviderType ProviderType => StorageProviderType.LocalStorage;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StorageUploadResponse> UploadAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var bucket = _options.S3.BucketName;
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var ext = Path.GetExtension(request.FileName);
        var fileKey = $"{datePrefix}/{Guid.CreateVersion7()}{ext}";

        var rootPath = _options.LocalStorage.RootPath;
        var fullPath = Path.Combine(rootPath, fileKey);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using (var destStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await request.FileStream.CopyToAsync(destStream, cancellationToken);
        }

        var fileInfo = new FileInfo(fullPath);
        var baseUrl = _options.LocalStorage.BaseUrl.TrimEnd('/');
        var relativeUrl = $"{baseUrl}/{fileKey}";

        return new StorageUploadResponse
        {
            FileKey = fileKey,
            BucketName = bucket,
            AccessUrl = relativeUrl,
            FileSize = fileInfo.Length
        };
    }

    public Task<Stream?> DownloadAsync(
        string fileKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.LocalStorage.RootPath, fileKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(
        string fileKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.LocalStorage.RootPath, fileKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(
        string fileKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.LocalStorage.RootPath, fileKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetPreSignedUrlAsync(
        string fileKey,
        TimeSpan expires,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _options.LocalStorage.BaseUrl.TrimEnd('/');
        return Task.FromResult($"{baseUrl}/{fileKey}");
    }

    public Task<List<PreSignedUrlResponseItem>> GetPreSignedUploadUrlsAsync(
        List<PreSignedUrlRequestItem> items,
        TimeSpan expires,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var bucket = bucketName ?? _options.S3.BucketName;
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var baseUrl = _options.LocalStorage.BaseUrl.TrimEnd('/');

        var results = new List<PreSignedUrlResponseItem>();

        foreach (var item in items)
        {
            var ext = Path.GetExtension(item.FileName);
            var fileKey = $"{datePrefix}/{Guid.CreateVersion7()}{ext}";
            var accessUrl = $"{baseUrl}/{fileKey}";

            results.Add(new PreSignedUrlResponseItem
            {
                FileName = item.FileName,
                FileKey = fileKey,
                UploadUrl = $"/api/v1/storage/upload?fileKey={Uri.EscapeDataString(fileKey)}",
                AccessUrl = accessUrl
            });
        }

        return Task.FromResult(results);
    }
}
