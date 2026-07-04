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
                 
                         .Map(dest => dest.UserFullName, src => src.User != null ? src.User.FirstName : "Unknown User")
                        .Map(dest => dest.UserFullName, src => src.User.FirstName + " " + src.User.LastName)
                        .Map(dest => dest.UserEmail, src => src.User.Email)
                        .Map(dest => dest.UserPhone, src => src.User.Phone);
            config.NewConfig<CreateSupportDto, Support>()

                .Map(dest => dest.Status, src => Support.SupportStatus.Open);

            config.NewConfig<UpdateSupportDto, Support>();



            //config.NewConfig<Support, ClientSupportResponse>()
            //    .Map(dest => dest.ClientName, src => src.User.FirstName + " " + src.User.LastName)
            //    .Map(dest => dest.ClientEmail, src => src.User.Email)
            //    .Map(dest => dest.ClientPhone, src => src.User.Phone);


        }
    }
}
