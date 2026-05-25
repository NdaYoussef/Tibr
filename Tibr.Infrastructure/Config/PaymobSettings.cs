namespace Tibr.Infrastructure.Config
{
    public class PaymobSettings
    {
        public const string SectionName = "Paymob";

        public string ApiKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string HmacSecret { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public string IframeId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://accept.paymob.com/api";
    }
}
