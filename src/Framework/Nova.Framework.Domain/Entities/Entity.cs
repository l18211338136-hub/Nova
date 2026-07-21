using System;

namespace Nova.Framework.Domain.Entities;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}

public abstract class Entity<TKey> : IEntity<TKey>
{
    public virtual TKey Id { get; set; } = default!;

    protected Entity()
    {
        if (typeof(TKey) == typeof(Guid))
        {
            Id = (TKey)(object)Guid.CreateVersion7();
        }
    }
}
