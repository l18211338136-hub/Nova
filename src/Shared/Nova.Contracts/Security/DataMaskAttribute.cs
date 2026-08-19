namespace Nova.Contracts.Security;

/// <summary>
/// 敏感数据脱敏类型
/// </summary>
public enum DataMaskType
{
    /// <summary>
    /// 手机号脱敏 (如: 138****1234)
    /// </summary>
    PhoneNumber,

    /// <summary>
    /// 邮箱脱敏 (如: a***b@gmail.com)
    /// </summary>
    Email,

    /// <summary>
    /// 身份证号脱敏 (如: 110101********1234)
    /// </summary>
    IdCard,

    /// <summary>
    /// 银行卡号脱敏 (如: 6222****1234)
    /// </summary>
    BankCard,

    /// <summary>
    /// 通用名字/姓名脱敏 (如: 张*三)
    /// </summary>
    ChineseName
}

/// <summary>
/// 敏感数据自动脱敏切面标记属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DataMaskAttribute : Attribute
{
    public DataMaskType MaskType { get; }

    public DataMaskAttribute(DataMaskType maskType)
    {
        MaskType = maskType;
    }
}
