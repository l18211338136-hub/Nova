using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Database;

namespace Nova.Modules.Storage.Application.Attachments.Queries;

public class GetAttachmentsQueryHandler : IConsumer<GetAttachmentsQuery>, IScopedDependency
{
    private readonly IStorageDbContext _db;
    private readonly IStorageProvider _storageProvider;

    public GetAttachmentsQueryHandler(IStorageDbContext db, IStorageProvider storageProvider)
    {
        _db = db;
        _storageProvider = storageProvider;
    }

    public async Task Consume(ConsumeContext<GetAttachmentsQuery> context)
    {
        var request = context.Message;

        var query = _db.Attachments
            .AsNoTracking()
            .Where(x => x.TargetType == request.TargetType && x.TargetId == request.TargetId);

        if (request.AttachmentType.HasValue)
        {
            query = query.Where(x => x.AttachmentType == request.AttachmentType.Value);
        }

        var attachments = await query
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(context.CancellationToken);

        if (attachments.Count == 0)
        {
            await context.RespondAsync(ApiResponse<List<AttachmentDto>>.Success(new List<AttachmentDto>()));
            return;
        }

        var fileIds = attachments.Select(x => x.FileId).Distinct().ToList();
        var files = await _db.FileObjects
            .AsNoTracking()
            .Where(x => fileIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, context.CancellationToken);

        var list = new List<AttachmentDto>();

        foreach (var att in attachments)
        {
            if (files.TryGetValue(att.FileId, out var fileObj))
            {
                // 运行时通过当前配置的 StorageProvider 动态生成包含最新 Host 域名的访问 URL 或 S3 临时预签名地址
                var accessUrl = await _storageProvider.GetPreSignedUrlAsync(fileObj.FileKey, TimeSpan.FromHours(24), fileObj.BucketName, context.CancellationToken);

                list.Add(new AttachmentDto
                {
                    Id = att.Id,
                    FileId = fileObj.Id,
                    TargetType = att.TargetType,
                    TargetId = att.TargetId,
                    AttachmentType = att.AttachmentType,
                    Sort = att.Sort,
                    Remarks = att.Remarks,
                    FileName = fileObj.FileName,
                    FileKey = fileObj.FileKey,
                    FileSize = fileObj.FileSize,
                    ContentType = fileObj.ContentType,
                    AccessUrl = accessUrl,
                    CreatedAt = att.CreatedAt
                });
            }
        }

        await context.RespondAsync(ApiResponse<List<AttachmentDto>>.Success(list));
    }
}
