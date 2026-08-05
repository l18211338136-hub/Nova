using Microsoft.EntityFrameworkCore;
using Nova.Modules.Audit.Domain.OperationLogs;

namespace Nova.Modules.Audit.Application.Database;

public interface IAuditDbContext
{
    DbSet<OperationLog> OperationLogs { get; }
    DbSet<SanitizationDetail> SanitizationDetails { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
