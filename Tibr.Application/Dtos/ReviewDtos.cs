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
