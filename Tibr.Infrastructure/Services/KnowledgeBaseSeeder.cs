using Tibr.Application.Services.AiChatServices;

namespace Tibr.Infrastructure.Services
{
    public static class KnowledgeBaseSeeder
    {
        public static List<FaqEntry> FaqEntries() =>
        [
            new("f1", "What is fractional gold investment?",
                "Fractional gold investment lets you buy a fraction of a gram of gold, "
                + "making gold accessible at any budget rather than requiring full gram purchases."),
            new("f2", "Is my gold physically stored?",
                "Yes. Tibr stores your gold in certified vaults. You own a verified fraction "
                + "of physical gold, not just a digital number."),
            new("f3", "How do I sell my gold on Tibr?",
                "You can sell any of your gold fractions at any time through the app. "
                + "The current market price is used and funds are credited to your wallet."),
            new("f4", "What is a gold fraction?",
                "A gold fraction is any amount of gold below one gram, such as 0.1g or 0.25g. "
                + "Tibr tracks ownership down to four decimal places."),
            new("f5", "Is Tibr compliant with Islamic finance principles?",
                "Tibr's model is designed to align with Islamic finance — ownership is real, "
                + "immediate, and asset-backed. Consult a scholar for your specific situation."),
            new("f6", "How does Tibr make money?",
                "Tibr earns a small commission on each transaction. "
                + "Premium agentic features are available via subscription."),
        ];

        public static List<FactEntry> FactEntries() =>
        [
            new("fc1", "Tibr's transaction commission is 0.8% per buy or sell order."),
            new("fc2", "The minimum purchase unit is 0.01 grams of gold."),
            new("fc3", "Withdrawals to a bank account are processed within 2 business days."),
            new("fc4", "Tibr currently supports gold only. Silver is planned for a future release."),
            new("fc5", "There is no monthly fee for a basic Tibr account."),
        ];
    }
}
