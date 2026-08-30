using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.Domain.SeedWork;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Roles.Commands;
using Nova.Modules.Identity.Application.Users.Commands;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using NSubstitute;
using Xunit;

namespace Nova.UnitTests.Handlers;

/// <summary>
/// 针对 BUG-04、BUG-05、BUG-06 的回归测试
/// </summary>
public class BugFixUserRoleTests
{
    private const string DefaultPassword = "Pass@123";

    private static async Task<User> SeedUserAsync(
        IdentityIntegrationHarness h, NovaTenantInfo tenant, string email)
    {
        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = User.Create(email, email);
        await um.CreateAsync(user, DefaultPassword);
        return user;
    }

    private static async Task SeedMappingAsync(IdentityIntegrationHarness h, string account, string tenantId)
    {
        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider);
        var db = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        db.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping { Account = account, TenantId = tenantId });
        await db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // BUG-04: 更新邮箱不同步 GlobalUserTenantMappings
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task BUG04_UpdateUser_EmailChanged_MappingUpdated()
    {
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;

        // 注册旧邮箱映射
        await SeedMappingAsync(h, "old-email@test.com", tenant.Identifier);
        var user = await SeedUserAsync(h, tenant, "old-email@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var tenantInfo = tenant as Finbuckle.MultiTenant.Abstractions.ITenantInfo;

        var handler = new UpdateUserCommandHandler(
            um, Substitute.For<IDomainEventDispatcher>(), tenantDb, tenant);

        var ctx = HandlerTestHarness.CreateConsumeContext(new UpdateUserCommand
        {
            Id = user.Id,
            UserName = "old-email@test.com",
            Email = "new-email@test.com",
            PhoneNumber = "",
            IsEnabled = true
        });
        await handler.Consume(ctx);

        // 旧邮箱映射应已删除
        var oldExists = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == "old-email@test.com" && m.TenantId == tenant.Identifier);
        Assert.False(oldExists);

        // 新邮箱映射应已创建
        var newExists = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == "new-email@test.com" && m.TenantId == tenant.Identifier);
        Assert.True(newExists);
    }

    [Fact]
    public async Task BUG04_UpdateUser_EmailUnchanged_MappingPreserved()
    {
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;

        await SeedMappingAsync(h, "unchanged@test.com", tenant.Identifier);
        var user = await SeedUserAsync(h, tenant, "unchanged@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var handler = new UpdateUserCommandHandler(
            um, Substitute.For<IDomainEventDispatcher>(), tenantDb, tenant);

        await handler.Consume(HandlerTestHarness.CreateConsumeContext(new UpdateUserCommand
        {
            Id = user.Id,
            UserName = "unchanged@test.com",
            Email = "unchanged@test.com", // 邮箱未变
            PhoneNumber = "",
            IsEnabled = true
        }));

        // 原映射应仍存在
        var exists = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == "unchanged@test.com" && m.TenantId == tenant.Identifier);
        Assert.True(exists);
    }

    // ═══════════════════════════════════════════════════════════
    // BUG-05: 删除用户不清理 GlobalUserTenantMappings
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task BUG05_DeleteUser_RemovesGlobalMappings()
    {
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;

        await SeedMappingAsync(h, "todelete@test.com", tenant.Identifier);
        var user = await SeedUserAsync(h, tenant, "todelete@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();

        var handler = new DeleteUserCommandHandler(um, dispatcher, tenantDb);
        await handler.Consume(HandlerTestHarness.CreateConsumeContext(new DeleteUserCommand
        {
            Id = user.Id
        }));

        // ★ 核心断言：用户删除后，邮箱映射也应清除（修复前孤儿数据残留）
        var stillExists = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == "todelete@test.com");
        Assert.False(stillExists);
    }

    [Fact]
    public async Task BUG05_DeleteUser_AfterDeletion_EmailCanBeReregistered()
    {
        // 验证删除后邮箱不再被 GlobalUserTenantMappings 占用
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;

        await SeedMappingAsync(h, "reuse@test.com", tenant.Identifier);
        var user = await SeedUserAsync(h, tenant, "reuse@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();

        var deleteHandler = new DeleteUserCommandHandler(um, Substitute.For<IDomainEventDispatcher>(), tenantDb);
        await deleteHandler.Consume(HandlerTestHarness.CreateConsumeContext(
            new DeleteUserCommand { Id = user.Id }));

        // 删除后，全局映射表中不应再有该邮箱
        var occupied = await tenantDb.GlobalUserTenantMappings
            .AnyAsync(m => m.Account == "reuse@test.com");
        Assert.False(occupied); // 修复前此处返回 true，导致邮箱永久无法重新注册
    }

    [Fact]
    public async Task BUG05_DeleteUser_DispatchesPermissionsUpdatedEvent()
    {
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;

        await SeedMappingAsync(h, "evtdel@test.com", tenant.Identifier);
        var user = await SeedUserAsync(h, tenant, "evtdel@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();

        var handler = new DeleteUserCommandHandler(um, dispatcher, tenantDb);
        await handler.Consume(HandlerTestHarness.CreateConsumeContext(
            new DeleteUserCommand { Id = user.Id }));

        await dispatcher.Received(1).PublishAsync(
            Arg.Is<Nova.Modules.Identity.Application.Events.UserPermissionsUpdatedEvent>(
                e => e.UserId == user.Id),
            Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════
    // BUG-06 角色侧: 更新菜单时应触发成员权限缓存失效
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task BUG06_UpdateRole_MenusChanged_DispatchesPermissionsUpdatedForMembers()
    {
        var h = IdentityIntegrationHarness.Create();
        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider);
        var rm = s.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dispatcher = h.Provider.GetRequiredService<IDomainEventDispatcher>();

        // 创建角色并分配用户
        var role = Role.Create("MenuRole", "菜单测试角色", null, 0);
        await rm.CreateAsync(role);
        await rm.AddClaimAsync(role, new Claim("Menu", "menu-old-001"));

        var member = User.Create("menumember@test.com", "menumember@test.com");
        await um.CreateAsync(member, DefaultPassword);
        await um.AddToRoleAsync(member, "MenuRole");

        var handler = new UpdateRoleCommandHandler(rm, um, dispatcher);
        var ctx = HandlerTestHarness.CreateConsumeContext(new UpdateRoleCommand
        {
            Id = role.Id,
            Name = "MenuRole",
            DisplayName = "菜单测试角色",
            Sort = 0,
            IsEnabled = true,
            // 只更新 Menus，不更新 Permissions
            Menus = new System.Collections.Generic.List<string> { "menu-new-001", "menu-new-002" }
        });

        await handler.Consume(ctx);

        // ★ 核心断言：即使只改了 Menus，也应触发缓存失效（修复前只有 Permissions 变更才触发）
        await dispatcher.Received(1).PublishAsync(
            Arg.Is<Nova.Modules.Identity.Application.Events.UserPermissionsUpdatedEvent>(
                e => e.UserId == member.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BUG06_UpdateRole_PermissionsChanged_StillDispatchesPermissionsUpdated()
    {
        // 回归测试：确保原有的 Permissions 变更也仍然触发缓存失效
        var h = IdentityIntegrationHarness.Create();
        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider);
        var rm = s.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dispatcher = h.Provider.GetRequiredService<IDomainEventDispatcher>();

        var role = Role.Create("PermRole", "权限测试角色", null, 0);
        await rm.CreateAsync(role);

        var member = User.Create("permmember@test.com", "permmember@test.com");
        await um.CreateAsync(member, DefaultPassword);
        await um.AddToRoleAsync(member, "PermRole");

        var handler = new UpdateRoleCommandHandler(rm, um, dispatcher);
        await handler.Consume(HandlerTestHarness.CreateConsumeContext(new UpdateRoleCommand
        {
            Id = role.Id,
            Name = "PermRole",
            DisplayName = "权限测试角色",
            Sort = 0,
            IsEnabled = true,
            Permissions = new System.Collections.Generic.List<string> { "Perm.New" }
        }));

        await dispatcher.Received(1).PublishAsync(
            Arg.Is<Nova.Modules.Identity.Application.Events.UserPermissionsUpdatedEvent>(
                e => e.UserId == member.Id),
            Arg.Any<CancellationToken>());
    }

    // BUG-06 用户侧: UpdateUserCommandHandler 也应在 Menus 变更时触发缓存失效
    [Fact]
    public async Task BUG06_UpdateUser_MenusChanged_DispatchesPermissionsUpdated()
    {
        var h = IdentityIntegrationHarness.Create();
        var tenant = h.CurrentTenant;
        var user = await SeedUserAsync(h, tenant, "usermenuperm@test.com");

        using var s = h.CreateScope();
        h.SetTenant(s.ServiceProvider, tenant);
        var um = s.ServiceProvider.GetRequiredService<UserManager<User>>();
        var tenantDb = s.ServiceProvider.GetRequiredService<NovaTenantDbContext>();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();

        var handler = new UpdateUserCommandHandler(um, dispatcher, tenantDb, tenant);
        await handler.Consume(HandlerTestHarness.CreateConsumeContext(new UpdateUserCommand
        {
            Id = user.Id,
            UserName = "usermenuperm@test.com",
            Email = "usermenuperm@test.com",
            PhoneNumber = "",
            IsEnabled = true,
            Menus = new System.Collections.Generic.List<string> { "menu-xyz" }
            // 不传 Permissions / Roles
        }));

        // ★ 核心断言：只更新 Menus 也应触发（修复前只有 Roles+Permissions 才触发）
        await dispatcher.Received(1).PublishAsync(
            Arg.Is<Nova.Modules.Identity.Application.Events.UserPermissionsUpdatedEvent>(
                e => e.UserId == user.Id),
            Arg.Any<Microsoft.EntityFrameworkCore.DbContext>(),
            Arg.Any<CancellationToken>());
    }
}
