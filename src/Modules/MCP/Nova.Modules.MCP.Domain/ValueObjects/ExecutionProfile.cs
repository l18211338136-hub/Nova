using System.Collections.Generic;

namespace Nova.Modules.Mcp.Domain.ValueObjects
{
    /// <summary>
    /// 运行时的“执行说明书”，在解析 OpenAPI 时生成。
    /// 记录了如何将大模型传入的扁平 JSON 参数，还原成真实的 HTTP 请求。
    /// </summary>
    public class ExecutionProfile
    {
        /// <summary>
        /// 目标 API 的完整路径 (例如: https://api.nova.com/api/users/{id})
        /// </summary>
        public string TargetUrl { get; set; } = string.Empty;

        /// <summary>
        /// 目标请求方法 (GET, POST, PUT, DELETE)
        /// </summary>
        public string TargetMethod { get; set; } = string.Empty;

        /// <summary>
        /// 鉴权令牌（仅作演示，实际应通过专门的 AuthHandler 处理）
        /// </summary>
        public string? AuthToken { get; set; }

        /// <summary>
        /// 参数映射表。Key: 大模型使用的参数名, Value: 映射规则
        /// </summary>
        public Dictionary<string, ParameterMapping> ParameterMap { get; set; } = new();
    }

    /// <summary>
    /// 单个参数的映射规则
    /// </summary>
    public class ParameterMapping
    {
        /// <summary>
        /// 目标 API 实际要求的参数名 (例如大模型传来 user_id，但真实接口要 userId)
        /// </summary>
        public string TargetKey { get; set; } = string.Empty;

        /// <summary>
        /// 参数存放的位置: "path", "query", "body", "header", "formData"
        /// </summary>
        public string In { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型标识，用于处理特殊类型（如 file 上传）
        /// </summary>
        public string Type { get; set; } = string.Empty;
    }
}
