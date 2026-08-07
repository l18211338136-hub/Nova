namespace Nova.Contracts.Storage;

public record StorageUploadRequest
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public string? BucketName { get; init; }
    public StorageAccessMode AccessMode { get; init; } = StorageAccessMode.Private;
}

public record StorageUploadResponse
{
    public required string FileKey { get; init; }
    public required string BucketName { get; init; }
    public required long FileSize { get; init; }
    public string? FileHash { get; init; }
    public required string AccessUrl { get; init; }
}

public record StorageFileDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = default!;
    public string FileKey { get; init; } = default!;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = default!;
    public string AccessUrl { get; init; } = default!;
}

public record PreSignedUrlRequestItem
{
    public required string FileName { get; init; }
    public string? ContentType { get; init; }
    public AttachmentType? AttachmentType { get; init; }
}

public record PreSignedUrlResponseItem
{
    public required string FileName { get; init; }
    public required string FileKey { get; init; }
    public required string UploadUrl { get; init; }
    public required string AccessUrl { get; init; }
}

public record AttachmentDto
{
    public Guid Id { get; init; }
    public Guid FileId { get; init; }
    public string TargetType { get; init; } = default!;
    public string TargetId { get; init; } = default!;
    public AttachmentType AttachmentType { get; init; }
    public int Sort { get; init; }
    public string? Remarks { get; init; }
    public string FileName { get; init; } = default!;
    public string FileKey { get; init; } = default!;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = default!;
    public string AccessUrl { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// 后端业务解耦事件：发布此事件自动绑定指定物理文件与业务实体（如用户头像、商品图库）
/// </summary>
public record BindAttachmentEvent
{
    public required Guid FileId { get; init; }
    public required string TargetType { get; init; }
    public required string TargetId { get; init; }
    public AttachmentType AttachmentType { get; init; } = AttachmentType.Attachment;
    public int Sort { get; init; }
    public string? Remarks { get; init; }
}
