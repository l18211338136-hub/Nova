using Nova.Contracts.CQRS;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Attachments.Commands;

[ApiEndpoint("POST", "/api/v1/storage/attachments/bind", typeof(ApiResponse<AttachmentDto>), "Storage", Summary = "将文件绑定为业务实体的附件（如用户头像、商品图片）")]
public record BindAttachmentCommand
{
    public Guid FileId { get; init; }
    public string TargetType { get; init; } = default!;
    public string TargetId { get; init; } = default!;
    public AttachmentType AttachmentType { get; init; } = AttachmentType.Attachment;
    public int Sort { get; init; } = 0;
    public string? Remarks { get; init; }
}
