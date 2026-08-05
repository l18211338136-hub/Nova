using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Nova.Contracts.Exceptions;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Application.Menus.Commands;
using Nova.Modules.Identity.Domain.Menus;
using Xunit;

namespace Nova.UnitTests.Handlers;

public class MenuCommandHandlersTests
{
    [Fact]
    public async Task CreateMenu_NewPath_CreatesMenu_And_Responds()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var handler = new CreateMenuCommandHandler(db);
        var cmd = new CreateMenuCommand { Name = "用户管理", Path = "/users", Component = "views/users", Sort = 1 };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);

        await ctx.Received(1).RespondAsync(Arg.Any<CreateMenuResult>());
        var saved = Assert.Single(await db.Menus.ToListAsync());
        Assert.Equal("/users", saved.Path);
        Assert.True(saved.IsEnabled);
    }

    [Fact]
    public async Task CreateMenu_DuplicatePath_Throws_NovaValidationException()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        db.Menus.Add(Menu.Create("已存在", "/users", "views/users"));
        await db.SaveChangesAsync();

        var handler = new CreateMenuCommandHandler(db);
        var cmd = new CreateMenuCommand { Name = "新菜单", Path = "/users", Component = "x" };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
        await ctx.DidNotReceive().RespondAsync(Arg.Any<object>());
    }

    [Fact]
    public async Task UpdateMenu_NotFound_Throws()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var handler = new UpdateMenuCommandHandler(db);
        var cmd = new UpdateMenuCommand { Id = System.Guid.NewGuid(), Name = "x", Path = "/x", Component = "c" };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task UpdateMenu_PathConflict_Throws()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var existing = Menu.Create("A", "/a", "c");
        var conflict = Menu.Create("B", "/b", "c");
        db.Menus.AddRange(existing, conflict);
        await db.SaveChangesAsync();

        var handler = new UpdateMenuCommandHandler(db);
        var cmd = new UpdateMenuCommand { Id = existing.Id, Name = "A2", Path = "/b", Component = "c" };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task UpdateMenu_Valid_Updates_And_Responds()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var menu = Menu.Create("A", "/a", "c");
        db.Menus.Add(menu);
        await db.SaveChangesAsync();

        var handler = new UpdateMenuCommandHandler(db);
        var cmd = new UpdateMenuCommand { Id = menu.Id, Name = "A2", Path = "/a2", Component = "c2", IsEnabled = false };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);
        await ctx.Received(1).RespondAsync(Arg.Any<UpdateMenuResult>());

        var updated = await db.Menus.SingleAsync(m => m.Id == menu.Id);
        Assert.Equal("/a2", updated.Path);
        Assert.False(updated.IsEnabled);
    }

    [Fact]
    public async Task DeleteMenu_NotFound_Throws()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var handler = new DeleteMenuCommandHandler(db);
        var cmd = new DeleteMenuCommand { Id = System.Guid.NewGuid() };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task DeleteMenu_HasChildren_Throws()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var parent = Menu.Create("P", "/p", "c");
        var child = Menu.Create("C", "/c", "c", parentId: parent.Id);
        db.Menus.AddRange(parent, child);
        await db.SaveChangesAsync();

        var handler = new DeleteMenuCommandHandler(db);
        var cmd = new DeleteMenuCommand { Id = parent.Id };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await Assert.ThrowsAsync<NovaValidationException>(() => handler.Consume(ctx));
    }

    [Fact]
    public async Task DeleteMenu_Valid_Completes_And_Responds()
    {
        var db = HandlerTestHarness.CreateInMemoryIdentityDb();
        var menu = Menu.Create("P", "/p", "c");
        db.Menus.Add(menu);
        await db.SaveChangesAsync();

        var handler = new DeleteMenuCommandHandler(db);
        var cmd = new DeleteMenuCommand { Id = menu.Id };
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);
        await ctx.Received(1).RespondAsync(Arg.Any<DeleteMenuResult>());

        var deletedMenu = await db.Menus.SingleOrDefaultAsync(m => m.Id == menu.Id);
        Assert.Null(deletedMenu);
    }
}
