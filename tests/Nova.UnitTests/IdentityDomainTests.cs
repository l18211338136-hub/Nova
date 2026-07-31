using Nova.Modules.Identity.Domain.Menus;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Xunit;

namespace Nova.UnitTests.Identity.Domain;

public class IdentityDomainTests
{
    [Fact]
    public void User_Create_Sets_UserName_Email_And_Defaults()
    {
        var user = User.Create("alice", "alice@example.com");

        Assert.Equal("alice", user.UserName);
        Assert.Equal("alice@example.com", user.Email);
        Assert.True(user.IsEnabled);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_Create_Throws_On_Invalid_UserName(string? userName)
    {
        Assert.ThrowsAny<ArgumentException>(() => User.Create(userName!, "alice@example.com"));
    }

    [Fact]
    public void User_Create_Throws_On_Invalid_Email()
    {
        Assert.Throws<ArgumentException>(() => User.Create("alice", ""));
    }

    [Fact]
    public void User_Enable_Disable_Toggles_State()
    {
        var user = User.Create("alice", "alice@example.com");

        user.Disable();
        Assert.False(user.IsEnabled);

        user.Enable();
        Assert.True(user.IsEnabled);
    }

    [Fact]
    public void Role_Create_Sets_Properties()
    {
        var role = Role.Create("Admin", "系统管理", "备注", 1);

        Assert.Equal("Admin", role.Name);
        Assert.Equal("系统管理", role.DisplayName);
        Assert.Equal(1, role.Sort);
        Assert.True(role.IsEnabled);
    }

    [Fact]
    public void Role_Create_Throws_On_Empty_Name()
    {
        Assert.Throws<ArgumentException>(() => Role.Create("", "系统管理", null, 0));
    }

    [Fact]
    public void Role_Update_Changes_Properties()
    {
        var role = Role.Create("Admin", "系统管理", null, 1);

        role.Update("Admin2", "管理2", "新备注", 2, false);

        Assert.Equal("Admin2", role.Name);
        Assert.Equal("管理2", role.DisplayName);
        Assert.Equal("新备注", role.Remarks);
        Assert.Equal(2, role.Sort);
        Assert.False(role.IsEnabled);
    }

    [Fact]
    public void Menu_Create_Sets_Properties_And_Parent()
    {
        var parentId = Guid.NewGuid();
        var menu = Menu.Create("用户管理", "/users", "user/index", "icon-user", parentId, 3);

        Assert.Equal("用户管理", menu.Name);
        Assert.Equal("/users", menu.Path);
        Assert.Equal("user/index", menu.Component);
        Assert.Equal("icon-user", menu.Icon);
        Assert.Equal(parentId, menu.ParentId);
        Assert.Equal(3, menu.Sort);
        Assert.True(menu.IsEnabled);
    }

    [Fact]
    public void Menu_Create_Throws_On_Empty_Path()
    {
        Assert.Throws<ArgumentException>(() => Menu.Create("用户管理", "", "user/index"));
    }

    [Fact]
    public void Menu_Update_Changes_Properties()
    {
        var menu = Menu.Create("用户管理", "/users", "user/index");

        menu.Update("用户管理2", "/u2", "c2", null, null, 5);

        Assert.Equal("用户管理2", menu.Name);
        Assert.Equal("/u2", menu.Path);
        Assert.Equal(5, menu.Sort);
        Assert.Null(menu.ParentId);
    }
}
