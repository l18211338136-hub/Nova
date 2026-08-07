using Nova.Contracts.CQRS;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Attachments.Queries;

[ApiEndpoint("GET", "/api/v1/storage/attachments", typeof(ApiResponse<List<AttachmentDto>>), "Storage", Summary = "查询业务实体的附件列表（如获取用户头像、商品轮播图）")]
public record GetAttachmentsQuery
{
    public string TargetType { get; init; } = default!;
    public string TargetId { get; init; } = default!;
    public AttachmentType? AttachmentType { get; init; }
}
