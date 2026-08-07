using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Database;
using Nova.Modules.Storage.Domain.Files;

namespace Nova.Modules.Storage.Application.Files.Commands;

public class InstantUploadCommandHandler : IConsumer<InstantUploadCommand>, IScopedDependency
{
    private readonly IStorageDbContext _db;
    private readonly ICurrentUser _currentUser;

    public InstantUploadCommandHandler(IStorageDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Consume(ConsumeContext<InstantUploadCommand> context)
    {
        var command = context.Message;

        if (string.IsNullOrWhiteSpace(command.FileHash))
        {
            await context.RespondAsync(ApiResponse<InstantUploadResult>.Success(new InstantUploadResult { IsInstant = false }));
            return;
        }

        var existingFile = await _db.FileObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileHash == command.FileHash && x.FileSize == command.FileSize, context.CancellationToken);

        if (existingFile != null)
        {
            // 云端/数据库中已存在相同 Hash & Size 的文件，秒传成功！
            var newFileObj = FileObject.Create(
                command.FileName,
                existingFile.FileKey,
                existingFile.BucketName,
                command.ContentType,
                command.FileSize,
                existingFile.Provider,
                existingFile.AccessMode,
                existingFile.FileHash,
                existingFile.AccessUrl
            );

            _db.FileObjects.Add(newFileObj);
            await _db.SaveChangesAsync(context.CancellationToken);

            await context.RespondAsync(ApiResponse<InstantUploadResult>.Success(new InstantUploadResult
            {
                IsInstant = true,
                FileId = newFileObj.Id,
                FileKey = newFileObj.FileKey,
                AccessUrl = newFileObj.AccessUrl
            }));
            return;
        }

        await context.RespondAsync(ApiResponse<InstantUploadResult>.Success(new InstantUploadResult { IsInstant = false }));
    }
}
