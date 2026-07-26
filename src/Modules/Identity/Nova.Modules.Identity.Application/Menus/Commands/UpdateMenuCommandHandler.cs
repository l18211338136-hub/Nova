using System.Threading.Tasks;
using MassTransit;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Menus;
using Nova.Modules.Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Nova.Modules.Identity.Application.Menus.Commands;

public class UpdateMenuCommandHandler : IConsumer<UpdateMenuCommand>
{
    private readonly IIdentityDbContext _db;

    public UpdateMenuCommandHandler(IIdentityDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<UpdateMenuCommand> context)
    {
        var command = context.Message;

        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.Id == command.Id && !m.IsDeleted, context.CancellationToken);
        if (menu == null)
        {
            throw new NovaValidationException($"菜单 {command.Id} 不存在");
        }

        var pathConflict = await _db.Menus.AnyAsync(m => m.Path == command.Path && m.Id != command.Id && !m.IsDeleted, context.CancellationToken);
        if (pathConflict)
        {
            throw new NovaValidationException($"菜单路由 '{command.Path}' 已被其他菜单使用");
        }

        menu.Update(
            command.Name,
            command.Path,
            command.Component,
            command.Icon,
            command.ParentId,
            command.Sort
        );
        
        menu.Remarks = command.Remarks;

        if (command.IsEnabled)
            menu.Enable();
        else
            menu.Disable();

        _db.Menus.Update(menu);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new UpdateMenuResult { MenuId = menu.Id });
    }
}
