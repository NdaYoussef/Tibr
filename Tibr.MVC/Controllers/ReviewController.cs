using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tibr.Application.Services.ReviewServices;
using Tibr.MVC.Models.Reviews;

namespace Tibr.MVC.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        // GET /Review — list all reviews, supports ?search=, ?rating=, ?page=
        public async Task<IActionResult> Index(string? search, int? rating, int page = 1)
        {
            var result = await _reviewService.GetAllForAdminAsync();
            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new ReviewListViewModel());
            }

            var allReviews = result.Data;

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                allReviews = allReviews.Where(r =>
                    r.UserFullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    r.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (rating.HasValue && rating > 0)
            {
                allReviews = allReviews.Where(r => r.Value == rating.Value);
            }

            var reviewList = allReviews.ToList();

            // Stats calculation before pagination
            var viewModel = new ReviewListViewModel
            {
                SearchKeyword = search,
                RatingFilter = rating,
                TotalReviews = reviewList.Count,
                AverageRating = reviewList.Any() ? Math.Round(reviewList.Average(r => r.Value), 1) : 0,
                FiveStarCount = reviewList.Count(r => r.Value == 5),
                FourStarCount = reviewList.Count(r => r.Value == 4),
                LowRatingCount = reviewList.Count(r => r.Value <= 2),
                TotalItems = reviewList.Count,
                CurrentPage = page
            };

            // Pagination
            var pagedReviews = reviewList
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(r => new ReviewRowViewModel
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    OrderNumber = r.OrderNumber,
                    UserId = r.UserId,
                    UserFullName = r.UserFullName,
                    UserEmail = r.UserEmail,
                    Description = r.Description,
                    Value = r.Value,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    TimeAgo = GetTimeAgo(r.CreatedAt)
                });

            viewModel.Reviews = pagedReviews;

            return View(viewModel);
        }

        // GET /Review/Details/{id} — view single review detail
        public async Task<IActionResult> Details(long id)
        {
            var result = await _reviewService.GetAllForAdminAsync();
            var review = result.Data?.FirstOrDefault(r => r.Id == id);

            if (review == null)
            {
                TempData["Error"] = "Review not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new ReviewDetailViewModel
            {
                Id = review.Id,
                OrderId = review.OrderId,
                OrderNumber = review.OrderNumber,
                UserFullName = review.UserFullName,
                UserEmail = review.UserEmail,
                Description = review.Description,
                Value = review.Value,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            return View(viewModel);
        }

        // POST /Review/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _reviewService.AdminDeleteAsync(id);
            if (result.IsSuccess)
            {
                TempData["Success"] = "Review deleted successfully.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;
            if (span.TotalDays > 30) return dateTime.ToString("MMM dd, yyyy");
            if (span.TotalDays > 1) return $"{(int)span.TotalDays} days ago";
            if (span.TotalHours > 1) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalMinutes > 1) return $"{(int)span.TotalMinutes} mins ago";
            return "Just now";
        }
    }
}