namespace Tibr.Application.Dtos;
public class CreateReviewDto
{
    public long OrderId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
}

public class UpdateReviewDto
{
    public string? Description { get; set; }
    public int? Value { get; set; }
}

public class ReviewDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminReviewDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}