using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;
using Nova.Modules.Notification.Infrastructure.Hubs;
using Nova.Modules.Notification.Infrastructure;

namespace Nova.Modules.Notification.Api;

public class NotificationModule : IModule
{
    public string Name => "Notification";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module specific services here
        services.AddSignalR();
        services.AddNotificationInfrastructure(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map module specific endpoints here
        endpoints.MapHub<NotificationHub>("/api/hubs/notifications");
        endpoints.MapNotificationEndpoints();
    }
}
