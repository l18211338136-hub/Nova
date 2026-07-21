using System;

namespace Nova.Framework.Domain.SeedWork;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
