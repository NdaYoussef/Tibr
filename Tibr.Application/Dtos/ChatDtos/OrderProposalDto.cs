namespace Tibr.Application.Dtos.ChatDtos
{
    public class OrderProposalDto
    {
        public string Action { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public decimal? AmountGrams { get; set; }
        public decimal? AmountEgp { get; set; }
        public decimal QuotedPricePerGram { get; set; }
        public decimal EstimatedTotalEgp { get; set; }
        public DateTime QuotedAt { get; set; }
    }
}
