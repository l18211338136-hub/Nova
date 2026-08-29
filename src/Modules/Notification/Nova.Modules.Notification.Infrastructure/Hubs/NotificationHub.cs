using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nova.Contracts.Constants;
using System.Security.Claims;

namespace Nova.Modules.Notification.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(TenantConstants.TenantIdClaimType)?.Value;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // 将用户加入其专属 Group (UserId)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            
            // 将用户加入其租户组，方便租户广播
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
            }
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirst(TenantConstants.TenantIdClaimType)?.Value;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
