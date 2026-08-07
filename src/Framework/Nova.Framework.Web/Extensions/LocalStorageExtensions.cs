using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Nova.Contracts.Storage;

namespace Nova.Framework.Web.Extensions;

public static class LocalStorageExtensions
{
    /// <summary>
    /// 当 ActiveProvider 为 Local 时，自动根据配置将本地物理路径挂载为静态 HTTP 访问目录
    /// </summary>
    public static IApplicationBuilder UseNovaLocalStorage(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<StorageOptions>>()?.Value;
        if (options == null) return app;

        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var rootPath = options.LocalStorage.RootPath;

        var uploadFolder = Path.IsPathRooted(rootPath)
            ? rootPath
            : Path.Combine(env.ContentRootPath, rootPath);

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        var requestPath = options.LocalStorage.BaseUrl.StartsWith('/')
            ? options.LocalStorage.BaseUrl
            : "/" + options.LocalStorage.BaseUrl;

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadFolder),
            RequestPath = requestPath
        });

        return app;
    }
}
