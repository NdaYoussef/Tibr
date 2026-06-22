using System.Text.Json;

namespace Tibr.Application.Services.AiChatServices.Tools
{
    public static class OrderBuilderTool
    {
        public static object FunctionDeclaration => new
        {
            name = "propose_order",
            description = "Propose a buy or sell order for the user to confirm. Only covers instant buy/sell — not conditional or recurring orders. Call this when the user explicitly asks to buy or sell gold/silver.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    action = new
                    {
                        type = "string",
                        @enum = new[] { "buy", "sell" },
                        description = "Whether the user wants to buy or sell"
                    },
                    asset = new
                    {
                        type = "string",
                        @enum = new[] { "gold", "silver" },
                        description = "The asset type"
                    },
                    scope = new
                    {
                        type = "string",
                        @enum = new[] { "specific_amount", "all_profitable", "all_holdings" },
                        description = "What to sell: a specific amount, or all profitable positions, or entire holdings"
                    },
                    amount_grams = new
                    {
                        type = "number",
                        description = "Quantity in grams (required when scope is 'specific_amount')"
                    },
                    amount_egp = new
                    {
                        type = "number",
                        description = "Amount in EGP to spend (alternative to amount_grams for buy orders)"
                    }
                },
                required = new[] { "action", "asset", "scope" }
            }
        };

        public static string[] RequiredProperties(string action, string scope)
        {
            if (scope == "specific_amount")
                return [action == "buy" ? "amount_egp or amount_grams" : "amount_grams"];
            return [];
        }

        public static (string Action, string Asset, string Scope, decimal? AmountGrams, decimal? AmountEgp) ParseArgs(string argsJson)
        {
            using var doc = JsonDocument.Parse(argsJson);
            var root = doc.RootElement;
            return (
                root.GetProperty("action").GetString()!,
                root.GetProperty("asset").GetString()!,
                root.GetProperty("scope").GetString()!,
                root.TryGetProperty("amount_grams", out var g) && g.ValueKind == JsonValueKind.Number ? g.GetDecimal() : null,
                root.TryGetProperty("amount_egp", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetDecimal() : null
            );
        }
    }
}
