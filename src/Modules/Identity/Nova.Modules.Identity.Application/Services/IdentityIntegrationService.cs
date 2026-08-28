using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Security;
using Nova.Modules.Identity.Application.Database;

namespace Nova.Modules.Identity.Application.Services;

public class IdentityIntegrationService : IIdentityIntegrationService
{
    private readonly IIdentityDbContext _db;

    public IdentityIntegrationService(IIdentityDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserSummaryDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (!ids.Any())
            return new List<UserSummaryDto>();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName,
                NickName = u.NickName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        return users;
    }
}
