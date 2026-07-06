using System.Collections.Generic;
using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.UserServices
{
    public interface IUserService
    {
        Task<Result<IEnumerable<UserListItemDto>>> GetUsersAsync(string? searchQuery, string? statusFilter, string? kycStatusFilter);
        Task<Result<UserDetailsDto>> GetByIdAsync(long id);
        Task<Result<UserDetailsDto>> UpdateAsync(long id, UpdateUserDto dto);
        Task<Result<string>> ToggleStatusAsync(long id);
        Task<Result<string>> UpdateKycStatusAsync(long id, string kycStatus);
        Task<Result<string>> DeleteAsync(long id);
    }
}