using Nova.Framework.Domain.SeedWork;

namespace Nova.Modules.Identity.Application.Events;

/// <summary>
/// 用户权限/角色变更领域事件
/// </summary>
public record UserPermissionsUpdatedEvent(Guid UserId) : IDomainEvent
{
    public DateTime OccurredOn => DateTime.UtcNow;
}
