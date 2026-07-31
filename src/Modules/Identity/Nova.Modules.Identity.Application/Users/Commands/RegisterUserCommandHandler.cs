using Nova.Contracts.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Nova.Contracts.Caching;
using Nova.Modules.Identity.Domain.Users;
using Nova.Framework.MultiTenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Nova.Modules.Identity.Application.Users.Commands;

public class RegisterUserCommandHandler : IConsumer<RegisterUserCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INovaCache _cache;
    private readonly NovaTenantDbContext _tenantDbContext;

    public RegisterUserCommandHandler(
        IServiceScopeFactory scopeFactory, 
        INovaCache cache,
        NovaTenantDbContext tenantDbContext)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _tenantDbContext = tenantDbContext;
    }

    public async Task Consume(ConsumeContext<RegisterUserCommand> context)
    {
        var command = context.Message;
        
        var cachedCode = await _cache.GetAsync<string>($"RegisterCode:{command.Email}");
        if (cachedCode != command.EmailCode)
        {
            throw new NovaValidationException("验证码错误或已过期");
        }

        // 验证成功后清理缓存
        await _cache.RemoveAsync($"RegisterCode:{command.Email}");

        // 3. 校验全局邮箱唯一性
        if (await _tenantDbContext.GlobalUserTenantMappings.AnyAsync(m => m.Account == command.Email))
        {
            throw new NovaValidationException("该邮箱已经被注册");
        }

        // 4. 为该注册用户生成全新的独立租户
        var tenantId = Guid.NewGuid().ToString("N");
        
        using var initScope = _scopeFactory.CreateScope();
        var configuration = initScope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        // 所有散户共享同一个数据库(nova_retail_db)，但通过 TenantId 字段进行逻辑隔离
        var retailConnectionString = configuration.GetConnectionString("RetailConnection");

        var tenantInfo = new NovaTenantInfo
        {
            Id = tenantId,
            Identifier = tenantId,
            Name = $"用户-{command.Username}",
            ConnectionString = retailConnectionString,
            AdminEmail = command.Email,
            AdminPassword = command.Password, // 传入密码，让初始化器直接使用该密码创建 Admin 账号
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(100)
        };

        _tenantDbContext.TenantInfo.Add(tenantInfo);
        await _tenantDbContext.SaveChangesAsync();

        // 强行把当前线程切到新租户，同步执行 EFCore 建表和种子数据填充
        var initSetter = initScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        initSetter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);

        var initializers = initScope.ServiceProvider.GetServices<Nova.Framework.MultiTenancy.IDbInitializer>();
        foreach (var initializer in initializers)
        {
            await initializer.MigrateAsync(default);
            await initializer.SeedAsync(default);
        }

        // 初始化器在 SeedAsync 时已经为该租户创建了管理员账号（即注册用户的邮箱），
        // 并且已经分配了 Admin 角色以及插入了 GlobalUserTenantMappings 映射，
        // 因此无需再手动 CreateAsync() 一次，避免"邮箱已存在"的错误。

        // 但我们需要获取刚创建的用户ID用于返回
        using var scope = _scopeFactory.CreateScope();
        var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = new MultiTenantContext<NovaTenantInfo>(tenantInfo);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        
        var user = await userManager.FindByEmailAsync(command.Email);

        await context.RespondAsync(new RegisterUserResult { UserId = user.Id });
    }
}
