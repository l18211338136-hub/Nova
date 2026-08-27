using Microsoft.EntityFrameworkCore;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Security;
using Nova.Modules.Organizations.Application.Database;

namespace Nova.Modules.Organizations.Infrastructure.Services;

/// <summary>
/// 组织机构模块实现的跨模块共享部门提供者（无反射，支持兼任多部门）
/// </summary>
public class UserOrganizationProvider : IUserOrganizationProvider, IScopedDependency
{
    private readonly IOrganizationDbContext _db;

    public UserOrganizationProvider(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> GetPrimaryOrgIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userOrg = await _db.UserOrganizations
            .AsNoTracking()
            .Where(uo => uo.UserId == userId)
            .OrderByDescending(uo => uo.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        return userOrg?.OrganizationId;
    }

    public async Task<List<Guid>> GetUserOrgIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserOrganizations
            .AsNoTracking()
            .Where(uo => uo.UserId == userId)
            .OrderByDescending(uo => uo.IsPrimary)
            .Select(uo => uo.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
