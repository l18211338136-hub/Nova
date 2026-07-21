using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;

namespace Nova.Modules.Billing.Api;

public class BillingModule : IModule
{
    public string Name => "Billing";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module specific services here
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map module specific endpoints here
    }
}
