using Mapster;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Mappers
{
    public class WalletMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Wallet, WalletBalanceDto>()
                .Map(dest => dest.AvailableBalance,
                    src => src.Balance - src.ReservedBalance);
        }
    }
}
