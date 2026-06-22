namespace Tibr.Application.Services.AiChatServices
{
    public record ClassificationResult(Intent Intent, double Confidence, string Reason);
}
