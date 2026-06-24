using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AddressServices
{
    public interface IAddressService
    {
        Task<Result<List<AddressDto>>> GetByUserAsync(long userId);
        Task<Result<AddressDto>> CreateAsync(CreateAddressDto dto);
        Task<Result> DeleteAsync(long addressId);
        Task<Result> SetDefaultAsync(long userId, long addressId);
    }
}
