using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Database;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
