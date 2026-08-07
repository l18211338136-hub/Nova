using Nova.Contracts.CQRS;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Attachments.Commands;

[ApiEndpoint("DELETE", "/api/v1/storage/attachments/{id}", typeof(ApiResponse<bool>), "Storage", Summary = "解绑/删除附件")]
public record UnbindAttachmentCommand
{
    public Guid Id { get; init; }
}
