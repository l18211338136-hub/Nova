using System.ComponentModel;
using Nova.Contracts.CQRS;
using Nova.Contracts.Idempotency;
using Nova.Contracts.Security;

namespace Nova.Modules.Identity.Application.TrashBin.Commands;

[ApiEndpoint("POST", "/api/identity/trash-bin/restore", typeof(RestoreTrashBinItemResult), "TrashBin", Summary = "恢复数据")]
[RequirePermission("Identity.TrashBin.Restore")]
[Idempotent(5)]
public record RestoreTrashBinItemCommand
{
    [Description("实体类型")]
    public string EntityType { get; init; } = default!;

    [Description("数据标识")]
    public Guid Id { get; init; }
}

public record RestoreTrashBinItemResult
{
    [Description("是否成功")]
    public bool Success { get; init; } = true;
}
