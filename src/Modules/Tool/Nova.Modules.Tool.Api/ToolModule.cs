using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;

namespace Nova.Modules.Tool.Api;

public class ToolModule : IModule
{
    public string Name => "Tool";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module specific services here
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map module specific endpoints here
    }
}
