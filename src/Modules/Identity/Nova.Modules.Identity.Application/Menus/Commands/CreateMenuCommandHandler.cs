using MassTransit;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Domain.Menus;
using Nova.Modules.Identity.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Nova.Modules.Identity.Application.Menus.Commands;

public class CreateMenuCommandHandler : IConsumer<CreateMenuCommand>
{
    private readonly IIdentityDbContext _db;

    public CreateMenuCommandHandler(IIdentityDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<CreateMenuCommand> context)
    {
        var command = context.Message;

        var existingMenu = await _db.Menus.FirstOrDefaultAsync(m => m.Path == command.Path && !m.IsDeleted, context.CancellationToken);
        if (existingMenu != null)
        {
            throw new NovaValidationException($"菜单路由 '{command.Path}' 已存在");
        }

        var menu = Menu.Create(
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

        _db.Menus.Add(menu);
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new CreateMenuResult { MenuId = menu.Id });
    }
}
