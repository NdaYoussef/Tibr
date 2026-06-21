using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class AssetPriceMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<AssetPrice, AssetPriceDto>();
        }
    }
}
