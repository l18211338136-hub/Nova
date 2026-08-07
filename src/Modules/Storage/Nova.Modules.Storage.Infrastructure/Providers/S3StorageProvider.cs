using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using Nova.Contracts.Storage;

namespace Nova.Modules.Storage.Infrastructure.Providers;

public class S3StorageProvider : IStorageProvider
{
    private readonly S3StorageOptions _options;
    private readonly IAmazonS3 _s3Client;

    public StorageProviderType ProviderType => StorageProviderType.MinIO;

    public S3StorageProvider(IOptions<StorageOptions> options)
    {
        _options = options.Value.S3;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = _options.ForcePathStyle,
            UseHttp = _options.UseHttp
        };

        if (!string.IsNullOrEmpty(_options.Region))
        {
            config.AuthenticationRegion = _options.Region;
        }

        _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<StorageUploadResponse> UploadAsync(StorageUploadRequest request, CancellationToken cancellationToken = default)
    {
        var bucketName = request.BucketName ?? _options.BucketName;
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        var extension = Path.GetExtension(request.FileName);
        var fileKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.CreateVersion7()}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileKey,
            InputStream = request.FileStream,
            ContentType = request.ContentType,
            AutoCloseStream = false
        };

        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        var accessUrl = $"/{bucketName}/{fileKey}";

        return new StorageUploadResponse
        {
            FileKey = fileKey,
            BucketName = bucketName,
            FileSize = request.FileStream.Length,
            AccessUrl = accessUrl
        };
    }

    public async Task<Stream?> DownloadAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetBucket = bucketName ?? _options.BucketName;
            var cleanKey = fileKey.StartsWith('/') ? fileKey.TrimStart('/') : fileKey;
            if (cleanKey.StartsWith($"{targetBucket}/", StringComparison.OrdinalIgnoreCase))
            {
                cleanKey = cleanKey.Substring(targetBucket.Length + 1);
            }

            var response = await _s3Client.GetObjectAsync(targetBucket, cleanKey, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetBucket = bucketName ?? _options.BucketName;
            await _s3Client.DeleteObjectAsync(targetBucket, fileKey, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string fileKey, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetBucket = bucketName ?? _options.BucketName;
            await _s3Client.GetObjectMetadataAsync(targetBucket, fileKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<string> GetPreSignedUrlAsync(string fileKey, TimeSpan expiresIn, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        var targetBucket = bucketName ?? _options.BucketName;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = targetBucket,
            Key = fileKey,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task<List<PreSignedUrlResponseItem>> GetPreSignedUploadUrlsAsync(
        List<PreSignedUrlRequestItem> items,
        TimeSpan expiresIn,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var targetBucket = bucketName ?? _options.BucketName;
        await EnsureBucketExistsAsync(targetBucket, cancellationToken);

        var results = new List<PreSignedUrlResponseItem>();

        foreach (var item in items)
        {
            var extension = Path.GetExtension(item.FileName);
            var fileKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.CreateVersion7()}{extension}";

            var request = new GetPreSignedUrlRequest
            {
                BucketName = targetBucket,
                Key = fileKey,
                Expires = DateTime.UtcNow.Add(expiresIn),
                Verb = HttpVerb.PUT,
                ContentType = item.ContentType ?? "application/octet-stream"
            };

            var uploadUrl = _s3Client.GetPreSignedURL(request);
            var accessUrl = $"/{targetBucket}/{fileKey}";

            results.Add(new PreSignedUrlResponseItem
            {
                FileName = item.FileName,
                FileKey = fileKey,
                UploadUrl = uploadUrl,
                AccessUrl = accessUrl
            });
        }

        return results;
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (!exists)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);
            }
        }
        catch
        {
            // 防并发建桶
        }
    }
}
