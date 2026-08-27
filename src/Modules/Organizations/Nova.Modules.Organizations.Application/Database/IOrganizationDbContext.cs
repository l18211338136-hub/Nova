using Microsoft.EntityFrameworkCore;
using Nova.Modules.Organizations.Domain;

namespace Nova.Modules.Organizations.Application.Database;

public interface IOrganizationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<UserOrganization> UserOrganizations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
