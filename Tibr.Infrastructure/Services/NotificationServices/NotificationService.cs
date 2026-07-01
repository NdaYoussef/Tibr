using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.NotificationsDtos;
using Tibr.Application.Interfaces;
using Tibr.Application.Services.NotificationServices;
using Tibr.Domain.Entities;
using Tibr.Domain.ResultPattern;
using Tibr.Infrastructure.Contexts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tibr.Infrastructure.Services.NotificationServices
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminNotificationPublisher _publisher;

        public NotificationService(ApplicationDbContext context, IAdminNotificationPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<Result<NotificationDto>> CreateAsync(CreateNotificationRequest request)
        {
            if (request.AdminId is long adminId)
            {
                var notification = BuildEntity(request, adminId);
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                var dto = notification.Adapt<NotificationDto>();
                await _publisher.PublishToAdminAsync(adminId, dto);

                return Result<NotificationDto>.Success(dto);
            }

            
            var adminIds = await _context.Admins
                .Select(a => a.Id)
                .ToListAsync();

            NotificationDto? lastDto = null;

            foreach (var id in adminIds)
            {
                var notification = BuildEntity(request, id);
                await _context.Notifications.AddAsync(notification);
                lastDto = notification.Adapt<NotificationDto>();
            }

            await _context.SaveChangesAsync();

            if (lastDto is not null)
                await _publisher.PublishToAllAdminsAsync(lastDto);

            return Result<NotificationDto>.Success(lastDto!);
        }

        public async Task<Result<List<NotificationDto>>> GetUnreadForAdminAsync(long adminId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.AdminId == adminId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Result<List<NotificationDto>>.Success(notifications.Adapt<List<NotificationDto>>());
        }

        public async Task<Result<List<NotificationDto>>> GetRecentForAdminAsync(long adminId, int count = 10)
        {
            var notifications = await _context.Notifications
                .Where(n => n.AdminId == adminId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();

            return Result<List<NotificationDto>>.Success(notifications.Adapt<List<NotificationDto>>());
        }

        public async Task<Result> MarkAsReadAsync(long notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification is null)
                return Result.Failure("Notification not found!");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> MarkAllAsReadAsync(long adminId)
        {
            await _context.Notifications
                .Where(n => n.AdminId == adminId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Result.Success();
        }

        private static Notification BuildEntity(CreateNotificationRequest request, long adminId) =>
            new()
            {
                AdminId = adminId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                RelatedEntityId = request.RelatedEntityId,
                ActionUrl = request.ActionUrl,
                IsRead = false
            };
    }

}
