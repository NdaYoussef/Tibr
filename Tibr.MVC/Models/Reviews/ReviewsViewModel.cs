using System;
using System.Collections.Generic;

namespace Tibr.MVC.Models.Reviews
{
    public class ReviewListViewModel
    {
        public IEnumerable<ReviewRowViewModel> Reviews { get; set; } = [];
        public string? SearchKeyword { get; set; }
        public int? RatingFilter { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }

        // Stat counts
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int LowRatingCount { get; set; }  // 1–2 stars
        public double AverageRating { get; set; }
    }

    public class ReviewRowViewModel
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Value { get; set; }  // 1–5
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // UI helpers
        public string StarDisplay => new string('★', Value) + new string('☆', 5 - Value);
        public string RatingClass => Value >= 4 ? "rating-high" : Value == 3 ? "rating-mid" : "rating-low";
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class ReviewDetailViewModel
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string StarDisplay => new string('★', Value) + new string('☆', 5 - Value);
    }
}