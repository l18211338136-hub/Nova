namespace Nova.Framework.Domain.Auditing;

/// <summary>
/// 禁用实体行级变更追溯审计特性。
/// 当标注在实体类型上时，EntityChangeCaptureInterceptor 将跳过对其变动 Diff 的捕获。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class DisableEntityChangeAuditingAttribute : Attribute
{
}
