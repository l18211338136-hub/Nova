using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Contracts.Security;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Database;
using Nova.Modules.Storage.Domain.Attachments;

namespace Nova.Modules.Storage.Application.Attachments.Commands;

public class BindAttachmentCommandHandler : IConsumer<BindAttachmentCommand>, IScopedDependency
{
    private readonly IStorageDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageProvider _storageProvider;

    public BindAttachmentCommandHandler(IStorageDbContext db, ICurrentUser currentUser, IStorageProvider storageProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _storageProvider = storageProvider;
    }

    public async Task Consume(ConsumeContext<BindAttachmentCommand> context)
    {
        var command = context.Message;

        var fileObj = await _db.FileObjects.FirstOrDefaultAsync(x => x.Id == command.FileId, context.CancellationToken);
        if (fileObj == null)
        {
            throw new NovaValidationException($"找不到 ID 为 '{command.FileId}' 的文件对象");
        }

        // 如果是单头附件类型（如 Avatar 头像），覆盖解绑之前的旧头像
        if (command.AttachmentType == AttachmentType.Avatar || command.AttachmentType == AttachmentType.Cover)
        {
            var oldAttachments = await _db.Attachments
                .Where(x => x.TargetType == command.TargetType && x.TargetId == command.TargetId && x.AttachmentType == command.AttachmentType)
                .ToListAsync(context.CancellationToken);

            if (oldAttachments.Count > 0)
            {
                _db.Attachments.RemoveRange(oldAttachments);
            }
        }

        var attachment = Attachment.Create(
            command.FileId,
            command.TargetType,
            command.TargetId,
            command.AttachmentType,
            command.Sort,
            command.Remarks
        );

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(context.CancellationToken);

        var accessUrl = fileObj.AccessUrl;
        if (string.IsNullOrEmpty(accessUrl))
        {
            accessUrl = await _storageProvider.GetPreSignedUrlAsync(fileObj.FileKey, TimeSpan.FromHours(24), fileObj.BucketName, context.CancellationToken);
        }

        var dto = new AttachmentDto
        {
            Id = attachment.Id,
            FileId = fileObj.Id,
            TargetType = attachment.TargetType,
            TargetId = attachment.TargetId,
            AttachmentType = attachment.AttachmentType,
            Sort = attachment.Sort,
            Remarks = attachment.Remarks,
            FileName = fileObj.FileName,
            FileKey = fileObj.FileKey,
            FileSize = fileObj.FileSize,
            ContentType = fileObj.ContentType,
            AccessUrl = accessUrl,
            CreatedAt = attachment.CreatedAt
        };

        await context.RespondAsync(ApiResponse<AttachmentDto>.Success(dto));
    }
}
