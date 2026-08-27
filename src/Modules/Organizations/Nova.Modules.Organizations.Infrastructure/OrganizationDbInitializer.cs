using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Organizations.Domain;

namespace Nova.Modules.Organizations.Infrastructure;

public class OrganizationDbInitializer : IDbInitializer, IScopedDependency
{
    private readonly OrganizationDbContext _dbContext;
    private readonly ILogger<OrganizationDbInitializer> _logger;

    public OrganizationDbInitializer(
        OrganizationDbContext dbContext,
        ILogger<OrganizationDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            _logger.LogInformation("Applying EF Core migrations for OrganizationDbContext...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedDefaultOrganizationsAsync(cancellationToken);
    }

    private async Task SeedDefaultOrganizationsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Organizations.AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding default organization tree nodes...");

        // 1. Root Headquarters
        var root = Organization.Create(
            name: "Nova 集团总部",
            parentId: null,
            code: "ROOT",
            type: "group",
            level: 1,
            sort: 1,
            remarks: "集团决策中心与战略管理总部");

        _dbContext.Organizations.Add(root);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. Child Companies (Level 2)
        var rdCompany = Organization.Create(
            name: "华东研发创新中心",
            parentId: root.Id,
            code: "COMPANY_RD",
            type: "company",
            level: 2,
            sort: 1,
            phone: "021-88888888",
            email: "rd_hub@nova.com",
            remarks: "云计算、ABAC安全架构及AI Agent核心研发基地");

        var opsCompany = Organization.Create(
            name: "华北运营管理中心",
            parentId: root.Id,
            code: "COMPANY_OPS",
            type: "company",
            level: 2,
            sort: 2,
            remarks: "市场拓展与客户服务中心");

        _dbContext.Organizations.AddRange(rdCompany, opsCompany);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Level 3 Departments
        var backendDept = Organization.Create(
            name: "后端架构部",
            parentId: rdCompany.Id,
            code: "DEPT_BACKEND",
            type: "department",
            level: 3,
            sort: 1,
            remarks: ".NET 10 模块化单体与微服务平台开发");

        var frontendDept = Organization.Create(
            name: "前端UI/UX体验中心",
            parentId: rdCompany.Id,
            code: "DEPT_FRONTEND",
            type: "department",
            level: 3,
            sort: 2,
            remarks: "React / Vite / TailwindCSS 管理端与小程序UI系统");

        var mktDept = Organization.Create(
            name: "数字化营销部",
            parentId: opsCompany.Id,
            code: "DEPT_MARKETING",
            type: "department",
            level: 3,
            sort: 1,
            remarks: "品牌推广与增长运营团队");

        _dbContext.Organizations.AddRange(backendDept, frontendDept, mktDept);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Organization seed data completed successfully.");
    }
}
