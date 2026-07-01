using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.NotificationsDtos;

namespace Tibr.Application.Interfaces
{
    public interface IAdminNotificationPublisher
    {

        Task PublishToAdminAsync(long adminId, NotificationDto notification);
        Task PublishToAllAdminsAsync(NotificationDto notification);
    }
}
