using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.AddressServices
{
    public class AddressService : IAddressService
    {
        private readonly IGenericRepository<Address, long> _addressRepo;

        public AddressService(IGenericRepository<Address, long> addressRepo)
        {
            _addressRepo = addressRepo;
        }

        public async Task<Result<List<AddressDto>>> GetByUserAsync(long userId)
        {
            var addresses = _addressRepo.GetAll(a => a.UserId == userId).ToList();

            var dtos = addresses.Where(a=>a.IsDeleted==false).Select(a => new AddressDto
            {
                Id = a.Id,
                PostalCode=a.PostalCode,
                City = a.City,
                IsDefault = a.IsDefault,
                FullAddress = $"{a.Building}, {a.Street}, {a.Area}, {a.City}"
                    + (string.IsNullOrEmpty(a.PostalCode) ? "" : $" ({a.PostalCode})")
            }).ToList();

            return Result<List<AddressDto>>.Success(dtos);
        }

        public async Task<Result<AddressDto>> CreateAsync(CreateAddressDto dto)
        {
            if (dto.IsDefault)
            {
                var existingDefault = _addressRepo.GetAll(a => a.UserId == dto.UserId && a.IsDefault).FirstOrDefault();
                if (existingDefault is not null)
                {
                    existingDefault.IsDefault = false;
                    await _addressRepo.UpdateAsync(existingDefault);
                }
            }

            var address = new Address
            {
                UserId = dto.UserId,
                City = dto.City,
                Area = dto.Area,
                Street = dto.Street,
                Building = dto.Building,
                PostalCode = dto.PostalCode,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
            };

            await _addressRepo.AddAsync(address);
            await _addressRepo.SaveChangesAsync();

            return Result<AddressDto>.Success(new AddressDto
            {
                Id = address.Id,
                City = address.City,
                IsDefault = address.IsDefault,
                FullAddress = $"{address.Building}, {address.Street}, {address.Area}, {address.City}"
                    + (string.IsNullOrEmpty(address.PostalCode) ? "" : $" ({address.PostalCode})")
            });
        }

        public async Task<Result> DeleteAsync(long addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address is null)
                return Result.Failure($"Address with ID {addressId} not found.");

            await _addressRepo.DeleteAsync(address);
            await _addressRepo.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> SetDefaultAsync(long userId, long addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address is null)
                return Result.Failure($"Address with ID {addressId} not found.");

            var existingDefault = _addressRepo.GetAll(a => a.UserId == userId && a.IsDefault).FirstOrDefault();
            if (existingDefault is not null && existingDefault.Id != addressId)
            {
                existingDefault.IsDefault = false;
                await _addressRepo.UpdateAsync(existingDefault);
            }

            address.IsDefault = true;
            await _addressRepo.UpdateAsync(address);
            await _addressRepo.SaveChangesAsync();

            return Result.Success();
        }
    }
}
