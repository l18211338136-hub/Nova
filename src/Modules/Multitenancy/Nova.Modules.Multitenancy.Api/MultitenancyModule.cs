using MassTransit.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;
using Nova.Modules.Multitenancy.Application.Features;

namespace Nova.Modules.Multitenancy.Api;

public class MultitenancyModule : IModule
{
    public string Name => "Multitenancy";

    public void RegisterServices(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/tenants", async (CreateTenantCommand request, IMediator mediator) =>
        {
            var client = mediator.CreateRequestClient<CreateTenantCommand>();
            var response = await client.GetResponse<CreateTenantResult>(request);
            return Results.Ok(new { TenantId = response.Message.TenantId });
        })
        .WithName("CreateTenant")
        .WithTags("Tenants")
        .WithSummary("创建租户")
        .WithDescription("初始化并创建一个新的租户")
        .Produces<string>(StatusCodes.Status200OK);
    }
}
