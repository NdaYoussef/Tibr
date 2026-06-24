using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class AddressMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Address, AddressDto>()
                .Map(dest => dest.FullAddress,
                    src => $"{src.Building}, {src.Street}, {src.Area}, {src.City}"
                        + (string.IsNullOrEmpty(src.PostalCode) ? "" : $" ({src.PostalCode})"));

            config.NewConfig<CreateAddressDto, Address>()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.IsDeleted)
                .Ignore(dest => dest.CreatedAt);
        }
    }
}
