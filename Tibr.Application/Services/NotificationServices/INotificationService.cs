using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.NotificationsDtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.NotificationServices
{
    public interface INotificationService
    {
        Task<Result<NotificationDto>> CreateAsync(CreateNotificationRequest request);
        Task<Result<List<NotificationDto>>> GetUnreadForAdminAsync(long adminId);
        Task<Result<List<NotificationDto>>> GetRecentForAdminAsync(long adminId, int count = 10);
        Task<Result> MarkAsReadAsync(long notificationId);
        Task<Result> MarkAllAsReadAsync(long adminId);
    }
}