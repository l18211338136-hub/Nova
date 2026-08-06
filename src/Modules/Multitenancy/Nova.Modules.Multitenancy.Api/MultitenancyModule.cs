using MassTransit.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;
using Nova.Framework.Web.Security;
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
        endpoints.MapMultitenancyODataEndpoints();

        endpoints.MapPost("/api/tenants", async (CreateTenantCommand request, IMediator mediator) =>
        {
            var client = mediator.CreateRequestClient<CreateTenantCommand>();
            var response = await client.GetResponse<CreateTenantResult>(request);
            return Results.Ok(new { response.Message.TenantId });
        })
        .WithName("CreateTenant")
        .WithTags("Tenants")
        .WithSummary("创建租户")
        .Produces<string>(StatusCodes.Status200OK)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Multitenancy.Tenants.Create"));

        endpoints.MapPut("/api/tenants/{id}", async (string id, UpdateTenantCommand request, IMediator mediator) =>
        {
            if (id != request.Id) return Results.BadRequest("Id mismatch");
            var client = mediator.CreateRequestClient<UpdateTenantCommand>();
            var response = await client.GetResponse<UpdateTenantResult>(request);
            return Results.Ok(new { response.Message.TenantId });
        })
        .WithName("UpdateTenant")
        .WithTags("Tenants")
        .WithSummary("更新租户")
        .Produces<string>(StatusCodes.Status200OK)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Multitenancy.Tenants.Update"));

        endpoints.MapDelete("/api/tenants/{id}", async (string id, IMediator mediator) =>
        {
            var client = mediator.CreateRequestClient<DeleteTenantCommand>();
            var response = await client.GetResponse<DeleteTenantResult>(new DeleteTenantCommand(id));
            return Results.Ok(new { response.Message.TenantId });
        })
        .WithName("DeleteTenant")
        .WithTags("Tenants")
        .WithSummary("删除租户")
        .Produces<string>(StatusCodes.Status200OK)
        .RequireAuthorization()
        .AddEndpointFilter(new PermissionFilter("Multitenancy.Tenants.Delete"));
    }
}
