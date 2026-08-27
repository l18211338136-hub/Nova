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

        // 3. 机构类型字典
        if (!await _dbContext.DictionaryTypes.AnyAsync(x => x.Code == "sys_org_type", cancellationToken))
        {
            var orgType = DictionaryType.Create(
                code: "sys_org_type",
                name: "机构类型",
                description: "组织架构节点类型 (集团/公司/事业部/部门/团队)",
                isSystem: true,
                isEnabled: true,
                sort: 3);

            _dbContext.DictionaryTypes.Add(orgType);

            _dbContext.DictionaryItems.AddRange(
                DictionaryItem.Create(orgType.Id, orgType.Code, "集团", "group", "primary", sort: 1, isDefault: true, isEnabled: true),
                DictionaryItem.Create(orgType.Id, orgType.Code, "公司 / 子公司", "company", "info", sort: 2, isDefault: false, isEnabled: true),
                DictionaryItem.Create(orgType.Id, orgType.Code, "事业部 / 中心", "division", "warning", sort: 3, isDefault: false, isEnabled: true),
                DictionaryItem.Create(orgType.Id, orgType.Code, "部门", "department", "success", sort: 4, isDefault: false, isEnabled: true),
                DictionaryItem.Create(orgType.Id, orgType.Code, "团队 / 小组", "team", "default", sort: 5, isDefault: false, isEnabled: true)
            );
        }

        // 4. 岗位职务字典 (sys_job_title)
        if (!await _dbContext.DictionaryTypes.AnyAsync(x => x.Code == "sys_job_title", cancellationToken))
        {
            var jobTitleType = DictionaryType.Create(
                code: "sys_job_title",
                name: "岗位职务",
                description: "组织机构成员岗位及职务配置",
                isSystem: true,
                isEnabled: true,
                sort: 4);

            _dbContext.DictionaryTypes.Add(jobTitleType);

            _dbContext.DictionaryItems.AddRange(
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "总经理 / CEO", "ceo", "danger", sort: 1, isDefault: false, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "部门总监", "director", "primary", sort: 2, isDefault: false, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "部门经理 / 负责人", "manager", "warning", sort: 3, isDefault: false, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "全栈/架构师", "architect", "info", sort: 4, isDefault: false, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "高级软件工程师", "senior_engineer", "success", sort: 5, isDefault: true, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "产品经理", "product_manager", "warning", sort: 6, isDefault: false, isEnabled: true),
                DictionaryItem.Create(jobTitleType.Id, jobTitleType.Code, "普通员工", "staff", "default", sort: 7, isDefault: false, isEnabled: true)
            );
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
