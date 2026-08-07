using Nova.Contracts.CQRS;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Files.Queries;

[ApiEndpoint("POST", "/api/v1/storage/presigned-urls", typeof(ApiResponse<List<PreSignedUrlResponseItem>>), "Storage", Summary = "批量获取 S3/MinIO/Local 预签名直传/上传 URL 数组")]
public record GetPreSignedUrlsQuery
{
    /// <summary>
    /// 请求的文件明细列表（支持 1 张或批量多张）
    /// </summary>
    public List<PreSignedUrlRequestItem> Items { get; init; } = new();

    /// <summary>
    /// 预签名 URL 有效期（分钟，默认 15 分钟）
    /// </summary>
    public int ExpiresInMinutes { get; init; } = 15;
}
