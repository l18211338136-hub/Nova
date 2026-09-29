using System.Threading;
using System.Threading.Tasks;

namespace Nova.Modules.Mcp.Application.Common.Interfaces
{
    public interface IMcpServerEngine
    {
        Task HoldSseConnectionAsync(string sessionId, Microsoft.AspNetCore.Http.HttpResponse response, CancellationToken cancellationToken);

        /// <summary>
        /// 处理一条 JSON-RPC 消息。
        /// <paramref name="mcpKey"/> 为本次请求使用的 MCP Key 明文，
        /// 用于「创建者关联」判定（公开工具 ∪ 该 Key 创建者自己的工具）。
        /// </summary>
        Task<bool> HandleMessageAsync(string sessionId, string? mcpKey, string jsonRpcMessage);
    }
}
