using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.NotificationsDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class NotificationDtos : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Notification, NotificationDto>()
                 .Map(dest => dest.Type, src => src.Type.ToString())
                 .Map(dest => dest.AdminId, src => src.AdminId)
                 .Map(dest => dest.Title, src => src.Title)
                 .Map(dest => dest.Message, src => src.Message)
                 .Map(dest => dest.IsRead, src => src.IsRead)
                 .Map(dest => dest.RelatedEntityId, src => src.RelatedEntityId);

            config.NewConfig<CreateNotificationRequest, Notification>();

        }
    }
}
