using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.CQRS;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Modules.Dictionary.Application.Database;
using Nova.Modules.Dictionary.Application.Dtos;
using Nova.Modules.Dictionary.Domain.DictionaryItems;

namespace Nova.Modules.Dictionary.Application.DictionaryItems.Commands;

[ApiEndpoint("POST", "/api/dictionary/items", typeof(ApiResponse<Guid>), "Dictionaries", Summary = "创建数据")]
[RequirePermission("Dictionary.Items.Create")]
public record CreateDictionaryItemCommand
{
    public Guid TypeId { get; init; }
    public string Label { get; init; } = default!;
    public string Value { get; init; } = default!;
    public string? TagType { get; init; }
    public int SortOrder { get; init; }
    public bool IsDefault { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? Remarks { get; init; }
}

[ApiEndpoint("PUT", "/api/dictionary/items/{Id}", typeof(ApiResponse<bool>), "Dictionaries", Summary = "更新数据")]
[RequirePermission("Dictionary.Items.Update")]
public record UpdateDictionaryItemCommand
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string Value { get; init; } = default!;
    public string? TagType { get; init; }
    public int SortOrder { get; init; }
    public bool IsDefault { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? Remarks { get; init; }
}

[ApiEndpoint("DELETE", "/api/dictionary/items/{Id}", typeof(ApiResponse<bool>), "Dictionaries", Summary = "删除数据")]
[RequirePermission("Dictionary.Items.Delete")]
public record DeleteDictionaryItemCommand
{
    public Guid Id { get; init; }
}

public class DictionaryItemCommandHandler :
    IConsumer<CreateDictionaryItemCommand>,
    IConsumer<UpdateDictionaryItemCommand>,
    IConsumer<DeleteDictionaryItemCommand>,
    IScopedDependency
{
    private readonly IDictionaryDbContext _db;

    public DictionaryItemCommandHandler(IDictionaryDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<CreateDictionaryItemCommand> context)
    {
        var msg = context.Message;

        var typeEntity = await _db.DictionaryTypes.FirstOrDefaultAsync(x => x.Id == msg.TypeId, context.CancellationToken);
        if (typeEntity == null)
        {
            throw new NovaValidationException("未找到对应的字典类型");
        }

        var exists = await _db.DictionaryItems
            .AnyAsync(x => x.TypeId == msg.TypeId && x.Value == msg.Value.Trim(), context.CancellationToken);
        if (exists)
        {
            throw new NovaValidationException($"字典项数据值 '{msg.Value}' 已存在");
        }

        if (msg.IsDefault)
        {
            var defaultItems = await _db.DictionaryItems
                .Where(x => x.TypeId == msg.TypeId && x.IsDefault)
                .ToListAsync(context.CancellationToken);
            foreach (var item in defaultItems)
            {
                item.Update(item.Label, item.Value, item.TagType, item.Sort, isDefault: false, item.IsEnabled, item.Remarks);
            }
        }

        var entity = DictionaryItem.Create(
            typeId: msg.TypeId,
            typeCode: typeEntity.Code,
            label: msg.Label,
            value: msg.Value,
            tagType: msg.TagType,
            sort: msg.SortOrder,
            isDefault: msg.IsDefault,
            isEnabled: msg.IsEnabled,
            remarks: msg.Remarks);

        _db.DictionaryItems.Add(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<Guid>.Success(entity.Id));
    }

    public async Task Consume(ConsumeContext<UpdateDictionaryItemCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.DictionaryItems.FirstOrDefaultAsync(x => x.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("未找到对应的字典项");
        }

        var exists = await _db.DictionaryItems
            .AnyAsync(x => x.TypeId == entity.TypeId && x.Id != msg.Id && x.Value == msg.Value.Trim(), context.CancellationToken);
        if (exists)
        {
            throw new NovaValidationException($"字典项数据值 '{msg.Value}' 已存在");
        }

        if (msg.IsDefault)
        {
            var defaultItems = await _db.DictionaryItems
                .Where(x => x.TypeId == entity.TypeId && x.Id != msg.Id && x.IsDefault)
                .ToListAsync(context.CancellationToken);
            foreach (var item in defaultItems)
            {
                item.Update(item.Label, item.Value, item.TagType, item.Sort, isDefault: false, item.IsEnabled, item.Remarks);
            }
        }

        entity.Update(
            label: msg.Label,
            value: msg.Value,
            tagType: msg.TagType,
            sort: msg.SortOrder,
            isDefault: msg.IsDefault,
            isEnabled: msg.IsEnabled,
            remarks: msg.Remarks);

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }

    public async Task Consume(ConsumeContext<DeleteDictionaryItemCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.DictionaryItems.FirstOrDefaultAsync(x => x.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("未找到对应的字典项");
        }

        _db.DictionaryItems.Remove(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }
}
