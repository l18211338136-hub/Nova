using Nova.Framework.Web.Modular;
using Nova.Modules.Notification.Infrastructure;

namespace Nova.Modules.Notification.Api;

public class NotificationModule : IModule
{
    public string Name => "Notification";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module specific services here
        services.AddNotificationInfrastructure(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map module specific endpoints here
    }
}
