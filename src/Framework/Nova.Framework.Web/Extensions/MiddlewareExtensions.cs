using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using System;

namespace Nova.Framework.Web.Extensions
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// 为特定路由单独放宽请求体大小限制。
        /// 必须在所有会读取 Request.Body 的中间件（如加解密、鉴权）之前调用。
        /// </summary>
        public static IApplicationBuilder UseEndpointRequestBodySizeLimit(
            this IApplicationBuilder app, 
            string pathPrefix, 
            long maxRequestBodySize)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(pathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var maxBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    if (maxBodySizeFeature is not null)
                    {
                        maxBodySizeFeature.MaxRequestBodySize = maxRequestBodySize;
                    }
                }

                await next();
            });
        }
    }
}
