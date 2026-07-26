using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Menus;

namespace Nova.Modules.Identity.Application.Database;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Menu> Menus { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
