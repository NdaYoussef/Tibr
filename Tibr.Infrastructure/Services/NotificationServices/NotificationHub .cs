using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Infrastructure.Services.NotificationServices
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins-all");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins-all");
            await base.OnDisconnectedAsync(exception);
        }

    }
}
