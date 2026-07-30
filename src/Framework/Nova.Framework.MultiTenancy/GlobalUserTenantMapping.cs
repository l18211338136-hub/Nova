using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nova.Framework.MultiTenancy;

[Table("GlobalUserTenantMappings", Schema = "system")]
public class GlobalUserTenantMapping
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户账号（通常为邮箱或用户名）
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Account { get; set; } = default!;

    /// <summary>
    /// 绑定的租户 ID
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TenantId { get; set; } = default!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
