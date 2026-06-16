using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ReviewServices;
public class ReviewService : IReviewService
{
    private readonly IGenericRepository<Review, long> _reviewRepo;
    private readonly IGenericRepository<Order, long> _orderRepo;

    public ReviewService(
        IGenericRepository<Review, long> reviewRepo,
        IGenericRepository<Order, long> orderRepo)
    {
        _reviewRepo = reviewRepo;
        _orderRepo = orderRepo;
    }

    public async Task<Result> CreateAsync(CreateReviewDto dto, long userId)
    {
        if (dto.Value < 1 || dto.Value > 5)
            return Result.Failure("Value must be between 1 and 5.");

        var exists = _reviewRepo
            .GetAll(r => r.OrderId == dto.OrderId && r.UserId == userId)
            .Any();

        if (exists)
            return Result.Failure("You have already reviewed this order.");

        var orderExists = _orderRepo
            .GetAll(o => o.Id == dto.OrderId && o.UserId == userId)
            .Any();

        if (!orderExists)
            return Result.Failure("Order not found or does not belong to you.");

        var review = new Review
        {
            OrderId = dto.OrderId,
            UserId = userId,
            Description = dto.Description,
            Value = dto.Value,
            CreatedAt = DateTime.UtcNow,
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long reviewId, UpdateReviewDto dto, long userId)
    {
        if (dto.Description is null && dto.Value is null)
            return Result.Failure("Nothing to update. Provide Description, Value, or both.");

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null)
            return Result.Failure("Review not found.");

        if (review.UserId != userId)
            return Result.Failure("You can only edit your own reviews.");

        if (dto.Description is not null)
            review.Description = dto.Description;

        if (dto.Value.HasValue)
        {
            if (dto.Value < 1 || dto.Value > 5)
                return Result.Failure("Value must be between 1 and 5.");
            review.Value = dto.Value.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<ReviewDto>> GetByUserIdAsync(long userId, long orderId)
    {
        var review = _reviewRepo.GetAll(r => r.UserId == userId && r.OrderId == orderId)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                Description = r.Description,
                Value = r.Value,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .FirstOrDefault();

        if (review is null)
            return Result<ReviewDto>.Failure("Review not found.");

        return Result<ReviewDto>.Success(review);
    }
}
