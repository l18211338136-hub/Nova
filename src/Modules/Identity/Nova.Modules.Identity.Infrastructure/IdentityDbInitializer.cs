using MassTransit.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Infrastructure;

public class IdentityDbInitializer : IDbInitializer, IScopedDependency
{
    private readonly IdentityDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IMediator _mediator;

    public IdentityDbInitializer(IdentityDbContext dbContext, UserManager<User> userManager, RoleManager<Role> roleManager, IMediator mediator)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _mediator = mediator;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        // 确保 Admin 角色存在
        if (await _roleManager.FindByNameAsync(NovaIdentityConstants.Roles.Admin) == null)
        {
            await _roleManager.CreateAsync(new Role { Name = NovaIdentityConstants.Roles.Admin, Remarks = "Administrator role with full permissions." });
        }

        // 确保 Root 角色存在
        if (await _roleManager.FindByNameAsync(NovaIdentityConstants.Roles.Root) == null)
        {
            await _roleManager.CreateAsync(new Role { Name = NovaIdentityConstants.Roles.Root, Remarks = "Super Administrator role for host tenant." });
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var tenantInfo = _dbContext.TenantInfo as NovaTenantInfo;

        // 如果是宿主租户 (root)，则创建固定的超级管理员
        if (tenantInfo?.Identifier == NovaIdentityConstants.Tenants.RootTenantId)
        {
            if (await _userManager.FindByEmailAsync(NovaIdentityConstants.Seed.RootEmail) == null)
            {
                var rootUser = new User
                {
                    UserName = NovaIdentityConstants.Seed.RootUserName,
                    Email = NovaIdentityConstants.Seed.RootEmail,
                    EmailConfirmed = true,
                    IsEnabled = true
                };

                var result = await _userManager.CreateAsync(rootUser, NovaIdentityConstants.Seed.RootPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(rootUser, NovaIdentityConstants.Roles.Root);
                    await _userManager.AddToRoleAsync(rootUser, NovaIdentityConstants.Roles.Admin);
                }
                else
                {
                    Console.WriteLine($"[Nova.Database] Failed to create root user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
        else
        {
            // 对于普通租户，尝试从当前的租户上下文中获取管理员邮箱
            var adminEmail = tenantInfo?.AdminEmail;

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                adminEmail = "admin@root.com"; // 容错后备方案
            }

            // 确保管理员账号存在
            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsEnabled = true
                };

                // 自动生成安全的随机初始密码
                var defaultPassword = GenerateRandomPassword();

                var result = await _userManager.CreateAsync(adminUser, defaultPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, NovaIdentityConstants.Roles.Admin);

                    // 使用注入的 MassTransit IMediator 发送欢迎邮件命令
                    var emailBody = $@"
                        <h3>欢迎加入 {tenantInfo?.Name ?? "Nova"}</h3>
                        <p>您的租户环境已初始化完毕。以下是您的初始管理员账号信息：</p>
                        <ul>
                            <li><strong>账号：</strong> {adminEmail}</li>
                            <li><strong>初始密码：</strong> {defaultPassword}</li>
                        </ul>
                        <p>请在首次登录后尽快修改您的密码以确保安全。</p>
                    ";

                    await _mediator.Send(new Nova.Contracts.Commands.SendEmailCommand(
                        To: adminEmail,
                        Subject: $"【Nova】您的管理员账号已创建",
                        Body: emailBody,
                        IsHtml: true
                    ), cancellationToken);
                }
                else
                {
                    Console.WriteLine($"[Nova.Database] Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private static string GenerateRandomPassword()
    {
        // 生成包含大小写字母、数字和特殊字符的随机密码，长度为 12
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string number = "1234567890";
        const string special = "!@#$%^&*";

        var random = new Random();
        var chars = new[]
        {
            lower[random.Next(lower.Length)],
            upper[random.Next(upper.Length)],
            number[random.Next(number.Length)],
            special[random.Next(special.Length)]
        }.ToList();

        const string all = lower + upper + number + special;
        for (int i = 4; i < 12; i++)
        {
            chars.Add(all[random.Next(all.Length)]);
        }

        // 打乱顺序
        return new string(chars.OrderBy(x => random.Next()).ToArray());
    }
}
