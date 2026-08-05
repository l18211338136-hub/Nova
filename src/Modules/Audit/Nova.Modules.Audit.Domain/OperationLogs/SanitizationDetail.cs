using Nova.Framework.Domain.Entities;

namespace Nova.Modules.Audit.Domain.OperationLogs;

/// <summary>
/// 敏感数据脱敏明细实体（所有非 Key 字段为 Nullable）
/// </summary>
public class SanitizationDetail : Entity<Guid>
{
    public Guid? LogId { get; private set; }
    public string? FieldName { get; private set; }
    public string? MaskedRule { get; private set; }
    public DateTime? SanitizedAt { get; private set; }

    private SanitizationDetail() { }

    public static SanitizationDetail Create(Guid? logId, string? fieldName, string? maskedRule)
    {
        return new SanitizationDetail
        {
            Id = Guid.CreateVersion7(),
            LogId = logId,
            FieldName = fieldName,
            MaskedRule = maskedRule,
            SanitizedAt = DateTime.UtcNow
        };
    }
}
