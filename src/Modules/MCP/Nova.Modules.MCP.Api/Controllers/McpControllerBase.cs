using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using System.Threading.Tasks;

namespace Nova.Modules.Mcp.Api.Controllers
{
    /// <summary>
    /// MCP 模块的基础控制器
    /// 封装了 MassTransit 的统一请求客户端，避免在子控制器中到处注入 IRequestClient
    /// </summary>
    [ApiController]
    public abstract class McpControllerBase : ControllerBase
    {
        private IScopedClientFactory? _clientFactory;

        /// <summary>
        /// 懒加载获取作用域内的 MassTransit ClientFactory
        /// </summary>
        protected IScopedClientFactory ClientFactory => 
            _clientFactory ??= HttpContext.RequestServices.GetRequiredService<IScopedClientFactory>();

        /// <summary>
        /// 统一封装的发送请求并获取响应的方法
        /// </summary>
        protected async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            var client = ClientFactory.CreateRequestClient<TRequest>();
            var response = await client.GetResponse<TResponse>(request);
            return response.Message;
        }
        
        /// <summary>
        /// 统一封装的发送事件方法 (Fire-and-forget)
        /// </summary>
        protected async Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class
        {
            var publishEndpoint = HttpContext.RequestServices.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(@event);
        }
    }
}
