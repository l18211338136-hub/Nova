using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Services;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Infrastructure;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 测试用 IDbInitializer：不触碰真实数据库，仅在内存 EF + Identity 中完成种子操作。
/// <para>
/// SeedAsync 会在调用方已切好租户上下文的 scope 内执行，按当前租户的 AdminEmail 创建
/// 管理员账号并写入 GlobalUserTenantMappings，与 RegisterUserCommandHandler 期望的
/// "种子器已建好账号"语义一致。
/// </para>
/// </summary>
public sealed class FakeDbInitializer : IDbInitializer
{
    private readonly IServiceProvider _sp;

    public FakeDbInitializer(IServiceProvider sp) => _sp = sp;

    public Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var accessor = _sp.GetRequiredService<IMultiTenantContextAccessor>();
        var tenant = accessor.MultiTenantContext?.TenantInfo as NovaTenantInfo;
        if (tenant is null) return;

        var um = _sp.GetRequiredService<UserManager<User>>();
        var rm = _sp.GetRequiredService<RoleManager<Role>>();

        // 确保 Admin 角色存在（生产种子器会创建），否则 AddToRoleAsync 会失败
        if (!await rm.RoleExistsAsync(NovaIdentityConstants.Roles.Admin))
            await rm.CreateAsync(Role.Create(NovaIdentityConstants.Roles.Admin, "管理员", null, 0));

        var user = User.Create(tenant.AdminEmail!, tenant.AdminEmail!);
        await um.CreateAsync(user, tenant.AdminPassword ?? "Pass@123");
        await um.AddToRoleAsync(user, NovaIdentityConstants.Roles.Admin);

        var tenantDb = _sp.GetRequiredService<NovaTenantDbContext>();
        tenantDb.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
        {
            Account = tenant.AdminEmail!,
            TenantId = tenant.Identifier!
        });
        await tenantDb.SaveChangesAsync(cancellationToken);
    }
}
