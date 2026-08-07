using Nova.Contracts.CQRS;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Files.Commands;

public record InstantUploadResult
{
    /// <summary>
    /// 是否秒传成功（true 表示免重复上传物理文件，已直接复用）
    /// </summary>
    public bool IsInstant { get; init; }

    /// <summary>
    /// 秒传成功后的文件 ID（若 IsInstant 为 false 则为空）
    /// </summary>
    public Guid? FileId { get; init; }

    /// <summary>
    /// 文件的物理路径 FileKey
    /// </summary>
    public string? FileKey { get; init; }

    /// <summary>
    /// 文件的访问 URL
    /// </summary>
    public string? AccessUrl { get; init; }
}

[ApiEndpoint("POST", "/api/v1/storage/instant-upload", typeof(ApiResponse<InstantUploadResult>), "Storage", Summary = "校验 Hash 进行文件秒传（若云端已存在相同 MD5 文件则免上传直接完成）")]
public record InstantUploadCommand
{
    public string FileHash { get; init; } = default!;
    public long FileSize { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
}
