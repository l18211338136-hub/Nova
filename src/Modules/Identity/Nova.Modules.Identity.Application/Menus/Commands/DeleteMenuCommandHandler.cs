using MassTransit;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Nova.Modules.Identity.Application.Menus.Commands;

public class DeleteMenuCommandHandler : IConsumer<DeleteMenuCommand>
{
    private readonly IIdentityDbContext _db;

    public DeleteMenuCommandHandler(IIdentityDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<DeleteMenuCommand> context)
    {
        var command = context.Message;

        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == command.Id, context.CancellationToken);
        if (menu == null)
        {
            throw new NovaValidationException($"菜单 {command.Id} 不存在");
        }

        var hasChildren = await _db.Menus.AnyAsync(m => m.ParentId == command.Id, context.CancellationToken);
        if (hasChildren)
        {
            throw new NovaValidationException($"该菜单下存在子菜单，请先删除子菜单");
        }

        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new DeleteMenuResult { Success = true });
    }
}
