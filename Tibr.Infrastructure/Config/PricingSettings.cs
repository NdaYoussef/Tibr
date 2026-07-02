namespace Tibr.Infrastructure.Config
{
    public class PricingSettings
    {
        public const string SectionName = "Pricing";

        public decimal Spread { get; set; } = 0.025m;
    }
}
