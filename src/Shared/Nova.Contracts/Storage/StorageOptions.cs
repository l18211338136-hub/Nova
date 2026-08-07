namespace Nova.Contracts.Storage;

/// <summary>
/// 存储配置根节点
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// 当前启用的存储提供商 ("MinIO", "S3", "Local")
    /// </summary>
    public string ActiveProvider { get; set; } = "MinIO";

    /// <summary>
    /// S3 / MinIO 兼容配置
    /// </summary>
    public S3StorageOptions S3 { get; set; } = new();

    /// <summary>
    /// 本地文件系统配置
    /// </summary>
    public LocalStorageOptions LocalStorage { get; set; } = new();
}

public class S3StorageOptions
{
    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "nova-storage";
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; } = true;
    public bool UseHttp { get; set; } = true;
}

public class LocalStorageOptions
{
    public string RootPath { get; set; } = "App_Data/Uploads";
    public string BaseUrl { get; set; } = "/uploads";
}
