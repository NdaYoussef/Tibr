using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.NotificationsDtos;
using Tibr.Application.Interfaces;

namespace Tibr.Infrastructure.Services.NotificationServices
{
    public class SignalRNotificationPublisher : IAdminNotificationPublisher
    {
        private readonly IHubContext<NotificationHub> _hub;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hub) => _hub = hub;


        public Task PublishToAdminAsync(long adminId, NotificationDto notification) =>
            _hub.Clients.User(adminId.ToString()).SendAsync("ReceiveNotification", notification);

        public Task PublishToAllAdminsAsync(NotificationDto notification) =>
            _hub.Clients.Group("admins-all").SendAsync("ReceiveNotification", notification);
    }
}
