using System.Threading;
using System.Threading.Tasks;

namespace Nova.Modules.Mcp.Application.Common.Interfaces
{
    public interface IMcpServerEngine
    {
        Task HoldSseConnectionAsync(string sessionId, Microsoft.AspNetCore.Http.HttpResponse response, CancellationToken cancellationToken);
        Task<bool> HandleMessageAsync(string sessionId, string jsonRpcMessage);
    }
}
