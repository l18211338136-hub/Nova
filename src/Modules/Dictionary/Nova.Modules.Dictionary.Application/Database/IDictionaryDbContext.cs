using Microsoft.EntityFrameworkCore;
using Nova.Modules.Dictionary.Domain.DictionaryItems;
using Nova.Modules.Dictionary.Domain.DictionaryTypes;

namespace Nova.Modules.Dictionary.Application.Database;

public interface IDictionaryDbContext
{
    DbSet<DictionaryType> DictionaryTypes { get; }
    DbSet<DictionaryItem> DictionaryItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
