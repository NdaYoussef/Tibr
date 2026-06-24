using System.ComponentModel.DataAnnotations;

namespace Tibr.Application.Dtos
{
    public class CreateStrategyFormDto
    {
        [Required]
        public string Asset { get; set; } = string.Empty;

        [Required]
        public string Side { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        public string Operator { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TargetPriceEgp { get; set; }

        [Required]
        public string ExecutionType { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
    }
}
