using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Result<IEnumerable<UserListItemDto>>> GetUsersAsync(string? searchQuery, string? statusFilter, string? kycStatusFilter)
        {
            try
            {
                var query = _userRepository.GetAll();

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var lowerSearch = searchQuery.ToLower();
                    query = query.Where(u => u.FirstName.ToLower().Contains(lowerSearch) ||
                                             u.LastName.ToLower().Contains(lowerSearch) ||
                                             u.Email.ToLower().Contains(lowerSearch) ||
                                             u.Phone.Contains(lowerSearch));
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = query.Where(u => u.Status == statusFilter);
                }

                if (!string.IsNullOrWhiteSpace(kycStatusFilter))
                {
                    query = query.Where(u => u.KycStatus == kycStatusFilter);
                }

                var users = await query.ToListAsync();
                var dtos = users.Adapt<IEnumerable<UserListItemDto>>();

                return Result<IEnumerable<UserListItemDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<UserListItemDto>>.Failure($"Error retrieving users: {ex.Message}");
            }
        }

        public async Task<Result<UserDetailsDto>> GetByIdAsync(long id)
        {
            try
            {
                var user = await _userRepository.GetAll()
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return Result<UserDetailsDto>.Failure("User not found");

                var dto = new UserDetailsDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Status = user.Status,
                    OtpVerified = user.OtpVerified,
                    KycStatus = user.KycStatus,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Orders = user.Orders.Select(o => new UserOrderHistoryDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        CreatedAt = o.CreatedAt
                    }).ToList()
                };

                return Result<UserDetailsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<UserDetailsDto>.Failure($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<Result<UserDetailsDto>> UpdateAsync(long id, UpdateUserDto dto)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null || user.IsDeleted)
                    return Result<UserDetailsDto>.Failure("User not found");

                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.Email = dto.Email;
                user.Phone = dto.Phone;
                user.Status = dto.Status;
                user.KycStatus = dto.KycStatus;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                var details = await GetByIdAsync(id);
                return details;
            }
            catch (Exception ex)
            {
                return Result<UserDetailsDto>.Failure($"Error updating user: {ex.Message}");
            }
        }

        public async Task<Result<string>> ToggleStatusAsync(long id)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null || user.IsDeleted)
                    return Result<string>.Failure("User not found");

                user.Status = user.Status == "Active" ? "Suspended" : "Active";
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return Result<string>.Success(user.Status);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error toggling user status: {ex.Message}");
            }
        }

        public async Task<Result<string>> UpdateKycStatusAsync(long id, string kycStatus)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null || user.IsDeleted)
                    return Result<string>.Failure("User not found");

                user.KycStatus = kycStatus;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return Result<string>.Success(user.KycStatus);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error updating KYC status: {ex.Message}");
            }
        }

        public async Task<Result<string>> DeleteAsync(long id)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null || user.IsDeleted)
                    return Result<string>.Failure("User not found");

                await _userRepository.DeleteAsync(user);
                await _userRepository.SaveChangesAsync();

                return Result<string>.Success("User soft deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error deleting user: {ex.Message}");
            }
        }
    }
}
