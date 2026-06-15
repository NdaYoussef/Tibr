using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ReviewServices;
public interface IReviewService
{
    Task<Result> CreateAsync(CreateReviewDto dto, long userId);
    Task<Result> UpdateAsync(long reviewId, UpdateReviewDto dto, long userId);
    Task<Result<List<ReviewDto>>> GetByUserIdAsync(long userId);
}
