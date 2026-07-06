using Tibr.Domain.Entities;
using Tibr.Domain.Enums;

namespace Tibr.Infrastructure.Seed
{
    public static class ProductCatalog
    {
        public static readonly (string Name, string Category, MetalType Metal, decimal Purity, decimal Weight, decimal BuyPrice, decimal SellPrice, long Stock)[] Products =
        [
            // Gold Ingots
            ("Gold Ingot 1g",              "Gold Ingots",       MetalType.Gold,   0.9999m,   1.000m,   4000m,   4100m,   5000),
            ("Gold Ingot 2.5g",            "Gold Ingots",       MetalType.Gold,   0.9999m,   2.500m,  10000m,  10250m,   3000),
            ("Gold Ingot 5g",              "Gold Ingots",       MetalType.Gold,   0.9999m,   5.000m,  20000m,  20500m,   2500),
            ("Gold Ingot 10g",             "Gold Ingots",       MetalType.Gold,   0.9999m,  10.000m,  40000m,  41000m,   2000),
            ("Gold Ingot 20g",             "Gold Ingots",       MetalType.Gold,   0.9999m,  20.000m,  80000m,  82000m,   1500),
            ("Gold Ingot 50g",             "Gold Ingots",       MetalType.Gold,   0.9999m,  50.000m, 200000m, 205000m,    800),
            ("Gold Ingot 100g",            "Gold Ingots",       MetalType.Gold,   0.9999m, 100.000m, 400000m, 410000m,    500),
            ("Gold Ingot 1oz",             "Gold Ingots",       MetalType.Gold,   0.9999m,  31.103m, 124412m, 127522m,   1000),

            // Silver Ingots
            ("Silver Ingot 10g",           "Silver Ingots",     MetalType.Silver, 0.9999m,  10.000m,    500m,    513m,  10000),
            ("Silver Ingot 20g",           "Silver Ingots",     MetalType.Silver, 0.9999m,  20.000m,   1000m,   1025m,   8000),
            ("Silver Ingot 50g",           "Silver Ingots",     MetalType.Silver, 0.9999m,  50.000m,   2500m,   2563m,   6000),
            ("Silver Ingot 100g",          "Silver Ingots",     MetalType.Silver, 0.9999m, 100.000m,   5000m,   5125m,   4000),
            ("Silver Ingot 250g",          "Silver Ingots",     MetalType.Silver, 0.9999m, 250.000m,  12500m,  12813m,   2000),
            ("Silver Ingot 500g",          "Silver Ingots",     MetalType.Silver, 0.9999m, 500.000m,  25000m,  25625m,   1000),
            ("Silver Ingot 1kg",           "Silver Ingots",     MetalType.Silver, 0.9999m,1000.000m,  50000m,  51250m,    500),
            ("Silver Ingot 1oz",           "Silver Ingots",     MetalType.Silver, 0.9999m,  31.103m,   1555m,   1594m,   5000),

            // Gold Coins
            ("Gold Coin 1oz",              "Gold Coins",        MetalType.Gold,   0.9999m,  31.103m, 124412m, 127522m,    800),
            ("Gold Coin 1/2oz",            "Gold Coins",        MetalType.Gold,   0.9999m,  15.552m,  62206m,  63761m,   1200),
            ("Gold Coin 1/4oz",            "Gold Coins",        MetalType.Gold,   0.9999m,   7.776m,  31103m,  31881m,   1500),
            ("Egyptian Gold Pound",        "Gold Coins",        MetalType.Gold,   0.8750m,   8.000m,  28000m,  28700m,   3000),

            // Silver Coins
            ("Silver Coin 1oz",            "Silver Coins",      MetalType.Silver, 0.9999m,  31.103m,   1555m,   1594m,   5000),
            ("Silver Coin 1/2oz",          "Silver Coins",      MetalType.Silver, 0.9999m,  15.552m,    778m,    797m,   6000),
            ("Silver Dirham",              "Silver Coins",      MetalType.Silver, 0.9990m,   2.975m,    149m,    153m,  10000),

            // Premium Collectibles
            ("Tutankhamun Gold Coin",      "Premium Collectibles", MetalType.Gold,   0.9999m,  31.103m, 130000m, 133250m,    200),
            ("Nefertiti Silver Round",     "Premium Collectibles", MetalType.Silver, 0.9999m,  31.103m,   1800m,   1845m,    500),
            ("Cleopatra Gold Coin",        "Premium Collectibles", MetalType.Gold,   0.9999m,  15.552m,  65000m,  66625m,    300),
            ("Pharaonic Limited Edition Gold Bar", "Premium Collectibles", MetalType.Gold, 0.9999m, 10.000m, 42000m, 43050m, 100),
            ("Egyptian Heritage Silver Bar", "Premium Collectibles", MetalType.Silver, 0.9999m, 100.000m,  5500m,   5638m,    300),
        ];

        public static readonly string[] Categories =
        [
            "Gold Ingots",
            "Silver Ingots",
            "Gold Coins",
            "Silver Coins",
            "Premium Collectibles",
        ];
    }
}
