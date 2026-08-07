using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Storage;
using Nova.Modules.Storage.Application.Database;
using Nova.Modules.Storage.Domain.Attachments;

namespace Nova.Modules.Storage.Application.Attachments.Events;

public class BindAttachmentEventConsumer : IConsumer<BindAttachmentEvent>, IScopedDependency
{
    private readonly IStorageDbContext _db;

    public BindAttachmentEventConsumer(IStorageDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<BindAttachmentEvent> context)
    {
        var evt = context.Message;

        var fileExists = await _db.FileObjects.AnyAsync(x => x.Id == evt.FileId, context.CancellationToken);
        if (!fileExists) return;

        // 如果是单附件类型（如 Avatar），自动覆写/解绑旧的头像附件关系
        if (evt.AttachmentType == AttachmentType.Avatar)
        {
            var oldAttachments = await _db.Attachments
                .Where(x => x.TargetType == evt.TargetType && x.TargetId == evt.TargetId && x.AttachmentType == evt.AttachmentType)
                .ToListAsync(context.CancellationToken);

            if (oldAttachments.Count > 0)
            {
                _db.Attachments.RemoveRange(oldAttachments);
            }
        }

        var attachment = Attachment.Create(
            evt.FileId,
            evt.TargetType,
            evt.TargetId,
            evt.AttachmentType,
            evt.Sort,
            evt.Remarks
        );

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
