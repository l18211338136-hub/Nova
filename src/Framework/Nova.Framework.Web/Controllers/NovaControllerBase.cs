using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using System.Threading.Tasks;

namespace Nova.Framework.Web.Controllers
{
    /// <summary>
    /// Nova 框架的标准控制器基类
    /// 封装了基于 MassTransit 的统一 CQRS 请求分发能力
    /// 业务模块中的所有 API Controller 都应继承此类
    /// </summary>
    [ApiController]
    public abstract class NovaControllerBase : ControllerBase
    {
        private IScopedClientFactory? _clientFactory;

        /// <summary>
        /// 懒加载获取作用域内的 MassTransit ClientFactory
        /// </summary>
        protected IScopedClientFactory ClientFactory => 
            _clientFactory ??= HttpContext.RequestServices.GetRequiredService<IScopedClientFactory>();

        /// <summary>
        /// 发送 Request/Response 模式的 CQRS 命令或查询
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
        /// 发送 Fire-and-forget 模式的事件发布
        /// </summary>
        protected async Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class
        {
            var publishEndpoint = HttpContext.RequestServices.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(@event);
        }
    }
}
