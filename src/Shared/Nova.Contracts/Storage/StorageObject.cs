namespace Nova.Contracts.Storage;

/// <summary>
/// 基础设施/契约级公共存储对象描述
/// </summary>
public class StorageObject
{
    public string ObjectKey { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long Size { get; set; }
    public string Url { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
