using Microsoft.EntityFrameworkCore;
using Nova.Contracts.CQRS;
using Nova.Contracts.Responses;
using Nova.Contracts.TrashBin;
using Nova.Framework.Domain.Auditing;

namespace Nova.Framework.Persistence.TrashBin;

public class TrashBinService : ITrashBinService
{
    private readonly DbContext _dbContext;

    public TrashBinService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<TrashBinItemDto>> GetDeletedItemsAsync(
        string? entityType = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var resultItems = new List<TrashBinItemDto>();
        var modelEntityTypes = _dbContext.Model.GetEntityTypes();

        foreach (var entityTypeInfo in modelEntityTypes)
        {
            var clrType = entityTypeInfo.ClrType;
            if (!typeof(IFullAuditedEntity).IsAssignableFrom(clrType)) continue;

            if (!string.IsNullOrWhiteSpace(entityType) &&
                !clrType.Name.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var method = typeof(TrashBinService)
                .GetMethod(nameof(GetDeletedForTypeAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(clrType);

            var items = await (Task<List<TrashBinItemDto>>)method.Invoke(this, new object[] { cancellationToken })!;
            resultItems.AddRange(items);
        }

        // 关联查询 User 账号信息
        var userIds = resultItems
            .Select(x => x.DeletedBy)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (userIds.Any())
        {
            var userEntityType = modelEntityTypes.FirstOrDefault(e => e.ClrType.Name == "User");
            if (userEntityType != null)
            {
                var usersSetMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                    .MakeGenericMethod(userEntityType.ClrType);
                var queryable = (IQueryable<object>)usersSetMethod.Invoke(_dbContext, null)!;

                var usersList = await queryable
                    .IgnoreQueryFilters()
                    .Where(u => userIds.Contains(EF.Property<Guid>(u, "Id")))
                    .Select(u => new
                    {
                        Id = EF.Property<Guid>(u, "Id"),
                        Account = EF.Property<string>(u, "UserName") ?? EF.Property<string>(u, "Email")
                    })
                    .ToListAsync(cancellationToken);

                var userDict = usersList.ToDictionary(x => x.Id, x => x.Account);

                foreach (var item in resultItems)
                {
                    if (item.DeletedBy.HasValue && userDict.TryGetValue(item.DeletedBy.Value, out var acc) && !string.IsNullOrWhiteSpace(acc))
                    {
                        item.DeletedByUserName = acc;
                    }
                    else
                    {
                        item.DeletedByUserName = item.DeletedBy.HasValue ? item.DeletedBy.Value.ToString() : "System";
                    }
                }
            }
        }
        else
        {
            foreach (var item in resultItems)
            {
                item.DeletedByUserName = "System";
            }
        }

        var total = resultItems.Count;
        var pagedItems = resultItems
            .OrderByDescending(x => x.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<TrashBinItemDto>
        {
            Items = pagedItems,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> RestoreItemAsync(string entityType, Guid id, CancellationToken cancellationToken = default)
    {
        var clrType = FindEntityType(entityType);
        if (clrType == null) return false;

        var method = typeof(TrashBinService)
            .GetMethod(nameof(RestoreForTypeAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(clrType);

        return await (Task<bool>)method.Invoke(this, new object[] { id, cancellationToken })!;
    }

    public async Task<bool> HardDeleteItemAsync(string entityType, Guid id, CancellationToken cancellationToken = default)
    {
        var clrType = FindEntityType(entityType);
        if (clrType == null) return false;

        var method = typeof(TrashBinService)
            .GetMethod(nameof(HardDeleteForTypeAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(clrType);

        return await (Task<bool>)method.Invoke(this, new object[] { id, cancellationToken })!;
    }

    private Type? FindEntityType(string entityName)
    {
        return _dbContext.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) && typeof(IFullAuditedEntity).IsAssignableFrom(t));
    }

    private async Task<List<TrashBinItemDto>> GetDeletedForTypeAsync<T>(CancellationToken cancellationToken) where T : class, IFullAuditedEntity
    {
        var deletedEntities = await _dbContext.Set<T>()
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);

        return deletedEntities.Select(e =>
        {
            var idProp = typeof(T).GetProperty("Id")?.GetValue(e);
            var nameProp = typeof(T).GetProperty("Name")?.GetValue(e)
                ?? typeof(T).GetProperty("UserName")?.GetValue(e)
                ?? typeof(T).GetProperty("DisplayName")?.GetValue(e)
                ?? idProp;

            return new TrashBinItemDto
            {
                Id = idProp is Guid g ? g : Guid.Empty,
                EntityType = typeof(T).Name,
                DisplayName = nameProp?.ToString() ?? typeof(T).Name,
                DeletedBy = e.DeletedBy,
                DeletedAt = e.DeletedAt
            };
        }).ToList();
    }

    private async Task<bool> RestoreForTypeAsync<T>(Guid id, CancellationToken cancellationToken) where T : class, IFullAuditedEntity
    {
        var entity = await _dbContext.Set<T>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.IsDeleted && EF.Property<Guid>(e, "Id") == id, cancellationToken);

        if (entity == null) return false;

        entity.IsDeleted = false;
        entity.DeletedBy = null;
        entity.DeletedAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> HardDeleteForTypeAsync<T>(Guid id, CancellationToken cancellationToken) where T : class, IFullAuditedEntity
    {
        var entity = await _dbContext.Set<T>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

        if (entity == null) return false;

        _dbContext.Set<T>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
