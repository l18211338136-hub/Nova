using System;

namespace Nova.Framework.Persistence.Outbox;

public class InboxMessage
{
    public Guid Id { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
