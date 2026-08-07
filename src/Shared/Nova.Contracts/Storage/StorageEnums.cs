namespace Nova.Contracts.Storage;

/// <summary>
/// 存储服务提供商枚举
/// </summary>
public enum StorageProviderType
{
    MinIO = 1,
    AWS_S3 = 2,
    LocalStorage = 3,
    AliyunOSS = 4,
    TencentCOS = 5
}

/// <summary>
/// 业务附件用途枚举
/// </summary>
public enum AttachmentType
{
    Avatar = 1,       // 用户/租户头像
    Cover = 2,        // 封面图
    Gallery = 3,      // 轮播图/商品画册
    Attachment = 4,   // 普通通用附件
    Document = 5,     // 文档 / 电子合同
    Image = 6,        // 独立图片
    Video = 7,        // 视频文件
    Audio = 8,        // 音频文件
    Other = 99        // 其他扩展类型
}

/// <summary>
/// 存储访问权限模式枚举
/// </summary>
public enum StorageAccessMode
{
    Private = 1,      // 私有受保护存储 (需预签名 URL 访问)
    PublicRead = 2    // 公开只读 (静态 Direct URL 访问)
}
