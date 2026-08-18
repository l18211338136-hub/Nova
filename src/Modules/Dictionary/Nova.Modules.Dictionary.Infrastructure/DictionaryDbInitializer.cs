using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Dictionary.Domain.DictionaryItems;
using Nova.Modules.Dictionary.Domain.DictionaryTypes;
using Nova.Modules.Dictionary.Infrastructure.Persistence;

namespace Nova.Modules.Dictionary.Infrastructure;

public class DictionaryDbInitializer : IDbInitializer, IScopedDependency
{
    private readonly DictionaryDbContext _dbContext;
    private readonly ILogger<DictionaryDbInitializer> _logger;

    public DictionaryDbInitializer(
        DictionaryDbContext dbContext,
        ILogger<DictionaryDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for DictionaryDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedDefaultDictionariesAsync(cancellationToken);
    }

    private async Task SeedDefaultDictionariesAsync(CancellationToken cancellationToken)
    {
        // 1. 性别字典
        if (!await _dbContext.DictionaryTypes.AnyAsync(x => x.Code == "sys_user_gender", cancellationToken))
        {
            var genderType = DictionaryType.Create(
                code: "sys_user_gender",
                name: "用户性别",
                description: "系统常用性别字典项",
                isSystem: true,
                isEnabled: true,
                sort: 1);

            _dbContext.DictionaryTypes.Add(genderType);

            _dbContext.DictionaryItems.AddRange(
                DictionaryItem.Create(genderType.Id, genderType.Code, "男", "M", "info", sort: 1, isDefault: true, isEnabled: true),
                DictionaryItem.Create(genderType.Id, genderType.Code, "女", "F", "danger", sort: 2, isDefault: false, isEnabled: true),
                DictionaryItem.Create(genderType.Id, genderType.Code, "未知", "U", "default", sort: 3, isDefault: false, isEnabled: true)
            );
        }

        // 2. 通用状态字典
        if (!await _dbContext.DictionaryTypes.AnyAsync(x => x.Code == "sys_common_status", cancellationToken))
        {
            var statusType = DictionaryType.Create(
                code: "sys_common_status",
                name: "通用数据状态",
                description: "通用启用/禁用状态",
                isSystem: true,
                isEnabled: true,
                sort: 2);

            _dbContext.DictionaryTypes.Add(statusType);

            _dbContext.DictionaryItems.AddRange(
                DictionaryItem.Create(statusType.Id, statusType.Code, "正常", "1", "success", sort: 1, isDefault: true, isEnabled: true),
                DictionaryItem.Create(statusType.Id, statusType.Code, "禁用", "0", "danger", sort: 2, isDefault: false, isEnabled: true)
            );
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
