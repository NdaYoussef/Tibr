using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ReviewServices;
public interface IReviewService
{
    Task<Result> CreateAsync(CreateReviewDto dto, long userId);
    Task<Result> UpdateAsync(long reviewId, UpdateReviewDto dto, long userId);
    Task<Result<ReviewDto>> GetByUserIdAsync(long userId,long orderId);

    // Admin Methods
    Task<Result<IEnumerable<AdminReviewDto>>> GetAllForAdminAsync();
    Task<Result> AdminDeleteAsync(long reviewId);
}
