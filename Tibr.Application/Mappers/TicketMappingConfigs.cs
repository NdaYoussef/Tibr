using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class TicketMappingConfigs : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Ticket, TicketDto>()
                  .Map(dest => dest.AdminName, src => src.Admin != null ? src.Admin.Name : "Unknown Admin")
                  .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            
             config.NewConfig<CreateTicketDto, Ticket>()
                 .Map(dest => dest.CreatedAt, src => DateTime.UtcNow);

        }
    }
}
