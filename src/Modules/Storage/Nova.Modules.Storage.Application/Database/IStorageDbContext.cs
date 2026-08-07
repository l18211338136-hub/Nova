using Microsoft.EntityFrameworkCore;
using Nova.Modules.Storage.Domain.Attachments;
using Nova.Modules.Storage.Domain.Files;

namespace Nova.Modules.Storage.Application.Database;

public interface IStorageDbContext
{
    DbSet<FileObject> FileObjects { get; }
    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
