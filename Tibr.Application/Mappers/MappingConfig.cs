using Mapster;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class MappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {

            #region Support Dtos
            config.NewConfig<CreateSupportRequestDto, Support>();
            config.NewConfig<UpdateSupportDto, Support>();
            config.NewConfig<SupportResponse, Support>(); 
            #endregion
        }
    }
}
