using Microsoft.EntityFrameworkCore;
using Nova.Framework.Domain.Auditing;
using Nova.Modules.Audit.Domain.OperationLogs;

namespace Nova.Modules.Audit.Application.Database;

public interface IAuditDbContext
{
    DbSet<OperationLog> OperationLogs { get; }
    DbSet<SanitizationDetail> SanitizationDetails { get; }
    DbSet<EntityChangeLog> EntityChangeLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
