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
            config.NewConfig<CreateSupportRequestDto, Support>();
            config.NewConfig<UpdateSupportDto, Support>();
            config.NewConfig<SupportResponse, Support>(); 
        }
    }
}
