using Nova.Contracts.Storage;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.Domain.Entities;

namespace Nova.Modules.Storage.Domain.Attachments;

public class Attachment : Entity<Guid>, IAuditedEntity
{
    public Guid FileId { get; private set; }
    public string TargetType { get; private set; } = default!;
    public string TargetId { get; private set; } = default!;
    public AttachmentType AttachmentType { get; private set; }
    public int Sort { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? ModifiedBy { get; set; }

    private Attachment() { }

    public static Attachment Create(
        Guid fileId,
        string targetType,
        string targetId,
        AttachmentType attachmentType,
        int sort = 0,
        string? remarks = null)
    {
        return new Attachment
        {
            Id = Guid.CreateVersion7(),
            FileId = fileId,
            TargetType = targetType,
            TargetId = targetId,
            AttachmentType = attachmentType,
            Sort = sort,
            Remarks = remarks,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateSort(int newSort)
    {
        Sort = newSort;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRemarks(string? remarks)
    {
        Remarks = remarks;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
