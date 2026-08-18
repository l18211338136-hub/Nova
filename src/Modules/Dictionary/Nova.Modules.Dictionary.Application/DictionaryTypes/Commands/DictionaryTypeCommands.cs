using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.CQRS;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Modules.Dictionary.Application.Database;
using Nova.Modules.Dictionary.Domain.DictionaryTypes;

namespace Nova.Modules.Dictionary.Application.DictionaryTypes.Commands;

[ApiEndpoint("POST", "/api/dictionary/types", typeof(ApiResponse<Guid>), "Dictionaries", Summary = "创建字典类型")]
[RequirePermission("Dictionary.Types.Create")]
public record CreateDictionaryTypeCommand
{
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
}

[ApiEndpoint("PUT", "/api/dictionary/types/{Id}", typeof(ApiResponse<bool>), "Dictionaries", Summary = "更新字典类型")]
[RequirePermission("Dictionary.Types.Update")]
public record UpdateDictionaryTypeCommand
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
}

[ApiEndpoint("DELETE", "/api/dictionary/types/{Id}", typeof(ApiResponse<bool>), "Dictionaries", Summary = "删除字典类型")]
[RequirePermission("Dictionary.Types.Delete")]
public record DeleteDictionaryTypeCommand
{
    public Guid Id { get; init; }
}

public class DictionaryTypeCommandHandler :
    IConsumer<CreateDictionaryTypeCommand>,
    IConsumer<UpdateDictionaryTypeCommand>,
    IConsumer<DeleteDictionaryTypeCommand>,
    IScopedDependency
{
    private readonly IDictionaryDbContext _db;

    public DictionaryTypeCommandHandler(IDictionaryDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<CreateDictionaryTypeCommand> context)
    {
        var msg = context.Message;
        var codeLower = msg.Code.Trim().ToLowerInvariant();

        var exists = await _db.DictionaryTypes.AnyAsync(x => x.Code == codeLower, context.CancellationToken);
        if (exists)
        {
            throw new NovaValidationException($"字典编码 '{msg.Code}' 已存在");
        }

        var entity = DictionaryType.Create(
            code: msg.Code,
            name: msg.Name,
            description: msg.Description,
            isSystem: msg.IsSystem,
            isEnabled: msg.IsEnabled,
            sort: msg.SortOrder);

        _db.DictionaryTypes.Add(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<Guid>.Success(entity.Id));
    }

    public async Task Consume(ConsumeContext<UpdateDictionaryTypeCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.DictionaryTypes.FirstOrDefaultAsync(x => x.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("未找到对应的字典类型");
        }

        entity.Update(
            name: msg.Name,
            description: msg.Description,
            isEnabled: msg.IsEnabled,
            sort: msg.SortOrder);

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }

    public async Task Consume(ConsumeContext<DeleteDictionaryTypeCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.DictionaryTypes.FirstOrDefaultAsync(x => x.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("未找到对应的字典类型");
        }

        if (entity.IsSystem)
        {
            throw new NovaValidationException("系统内置字典禁止删除");
        }

        var items = await _db.DictionaryItems.Where(x => x.TypeId == entity.Id).ToListAsync(context.CancellationToken);
        if (items.Count > 0)
        {
            _db.DictionaryItems.RemoveRange(items);
        }

        _db.DictionaryTypes.Remove(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }
}
