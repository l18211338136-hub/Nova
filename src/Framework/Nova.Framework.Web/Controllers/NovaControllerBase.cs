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
        /// <param name="request">CQRS 命令/查询对象</param>
        /// <param name="requestTimeout">
        /// 可选的中介请求超时。长耗时命令（如拉取数兆 Swagger 文档）需显式放宽，
        /// 否则沿用 MassTransit 默认 30s 超时，大文档在高延迟网络下会被提前中止。
        /// </param>
        protected async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? requestTimeout = null)
            where TRequest : class
            where TResponse : class
        {
            var client = requestTimeout.HasValue
                ? ClientFactory.CreateRequestClient<TRequest>(requestTimeout.Value)
                : ClientFactory.CreateRequestClient<TRequest>();
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
