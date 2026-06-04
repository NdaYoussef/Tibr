using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class SupportMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Support, SupportResponse>()

                  .Map(dest => dest.Status, src => src.Status.ToString())
                  .Map(dest => dest.UserFullName, src => src.User != null ? src.User.FirstName : "Unknown User");

            config.NewConfig<CreateSupportDto, Support>()

                .Map(dest => dest.Status, src => Support.SupportStatus.Open);

            config.NewConfig<UpdateSupportDto, Support>();
              

        }
    }
}
