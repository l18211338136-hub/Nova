using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Database;

namespace Nova.Modules.Storage.Application.Attachments.Commands;

public class UnbindAttachmentCommandHandler : IConsumer<UnbindAttachmentCommand>, IScopedDependency
{
    private readonly IStorageDbContext _db;

    public UnbindAttachmentCommandHandler(IStorageDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<UnbindAttachmentCommand> context)
    {
        var command = context.Message;

        var attachment = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == command.Id, context.CancellationToken);
        if (attachment == null)
        {
            throw new NovaValidationException($"找不到 ID 为 '{command.Id}' 的附件关联");
        }

        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }
}
