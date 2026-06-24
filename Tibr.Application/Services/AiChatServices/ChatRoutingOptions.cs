namespace Tibr.Application.Services.AiChatServices;

public class ChatRoutingOptions
{
    public const string SectionName = "ChatRouting";
    public double DirectHitConfidenceThreshold { get; set; } = 0.85;
    public int DualRagTopK { get; set; } = 3;
}
