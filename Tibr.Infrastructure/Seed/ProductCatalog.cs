using Tibr.Domain.Entities;
using Tibr.Domain.Enums;

namespace Tibr.Infrastructure.Seed
{
    public static class ProductCatalog
    {
        public static readonly (string NameAr, string NameEn, string CategoryAr, string CategoryEn, MetalType Metal, decimal Purity, decimal Weight, decimal BuyPrice, decimal SellPrice, long Stock)[] Products =
        [
            // Gold Ingots
            ("سبيكة ذهب 1 جرام",             "Gold Ingot 1g",              "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,   1.000m,   4000m,   4100m,   5000),
            ("سبيكة ذهب 2.5 جرام",           "Gold Ingot 2.5g",            "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,   2.500m,  10000m,  10250m,   3000),
            ("سبيكة ذهب 5 جرام",             "Gold Ingot 5g",              "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,   5.000m,  20000m,  20500m,   2500),
            ("سبيكة ذهب 10 جرام",            "Gold Ingot 10g",             "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,  10.000m,  40000m,  41000m,   2000),
            ("سبيكة ذهب 20 جرام",            "Gold Ingot 20g",             "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,  20.000m,  80000m,  82000m,   1500),
            ("سبيكة ذهب 50 جرام",            "Gold Ingot 50g",             "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,  50.000m, 200000m, 205000m,    800),
            ("سبيكة ذهب 100 جرام",           "Gold Ingot 100g",            "سبائك",       "Ingots",            MetalType.Gold,   0.9999m, 100.000m, 400000m, 410000m,    500),
            ("سبيكة ذهب 1 أونصة",            "Gold Ingot 1oz",             "سبائك",       "Ingots",            MetalType.Gold,   0.9999m,  31.103m, 124412m, 127522m,   1000),

            // Silver Ingots
            ("سبيكة فضة 10 جرام",            "Silver Ingot 10g",           "سبائك",       "Ingots",            MetalType.Silver, 0.9999m,  10.000m,    500m,    513m,  10000),
            ("سبيكة فضة 20 جرام",            "Silver Ingot 20g",           "سبائك",       "Ingots",            MetalType.Silver, 0.9999m,  20.000m,   1000m,   1025m,   8000),
            ("سبيكة فضة 50 جرام",            "Silver Ingot 50g",           "سبائك",       "Ingots",            MetalType.Silver, 0.9999m,  50.000m,   2500m,   2563m,   6000),
            ("سبيكة فضة 100 جرام",           "Silver Ingot 100g",          "سبائك",       "Ingots",            MetalType.Silver, 0.9999m, 100.000m,   5000m,   5125m,   4000),
            ("سبيكة فضة 250 جرام",           "Silver Ingot 250g",          "سبائك",       "Ingots",            MetalType.Silver, 0.9999m, 250.000m,  12500m,  12813m,   2000),
            ("سبيكة فضة 500 جرام",           "Silver Ingot 500g",          "سبائك",       "Ingots",            MetalType.Silver, 0.9999m, 500.000m,  25000m,  25625m,   1000),
            ("سبيكة فضة 1 كيلو",             "Silver Ingot 1kg",           "سبائك",       "Ingots",            MetalType.Silver, 0.9999m,1000.000m,  50000m,  51250m,    500),
            ("سبيكة فضة 1 أونصة",            "Silver Ingot 1oz",           "سبائك",       "Ingots",            MetalType.Silver, 0.9999m,  31.103m,   1555m,   1594m,   5000),

            // Gold Coins
            ("عملة ذهب 1 أونصة",             "Gold Coin 1oz",              "عملات",       "Coins",             MetalType.Gold,   0.9999m,  31.103m, 124412m, 127522m,    800),
            ("عملة ذهب نصف أونصة",           "Gold Coin 1/2oz",            "عملات",       "Coins",             MetalType.Gold,   0.9999m,  15.552m,  62206m,  63761m,   1200),
            ("عملة ذهب ربع أونصة",           "Gold Coin 1/4oz",            "عملات",       "Coins",             MetalType.Gold,   0.9999m,   7.776m,  31103m,  31881m,   1500),
            ("جنيه ذهب مصري",                "Egyptian Gold Pound",        "عملات",       "Coins",             MetalType.Gold,   0.8750m,   8.000m,  28000m,  28700m,   3000),

            // Silver Coins
            ("عملة فضة 1 أونصة",             "Silver Coin 1oz",            "عملات",       "Coins",             MetalType.Silver, 0.9999m,  31.103m,   1555m,   1594m,   5000),
            ("عملة فضة نصف أونصة",           "Silver Coin 1/2oz",          "عملات",       "Coins",             MetalType.Silver, 0.9999m,  15.552m,    778m,    797m,   6000),
            ("درهم فضة",                      "Silver Dirham",              "عملات",       "Coins",             MetalType.Silver, 0.9990m,   2.975m,    149m,    153m,  10000),

            // Premium Collectibles
            ("عملة ذهب توت عنخ آمون",        "Tutankhamun Gold Coin",      "مقتنيات فاخرة", "Premium Collectibles", MetalType.Gold,   0.9999m,  31.103m, 130000m, 133250m,    200),
            ("قرص فضة نفرتيتي",              "Nefertiti Silver Round",     "مقتنيات فاخرة", "Premium Collectibles", MetalType.Silver, 0.9999m,  31.103m,   1800m,   1845m,    500),
            ("عملة ذهب كليوباترا",            "Cleopatra Gold Coin",        "مقتنيات فاخرة", "Premium Collectibles", MetalType.Gold,   0.9999m,  15.552m,  65000m,  66625m,    300),
            ("سبيكة ذهب فرعونية إصدار محدود", "Pharaonic Limited Edition Gold Bar", "مقتنيات فاخرة", "Premium Collectibles", MetalType.Gold, 0.9999m, 10.000m, 42000m, 43050m, 100),
            ("سبيكة فضة التراث المصري",       "Egyptian Heritage Silver Bar", "مقتنيات فاخرة", "Premium Collectibles", MetalType.Silver, 0.9999m, 100.000m,  5500m,   5638m,    300),
        ];

        public static readonly (string NameAr, string NameEn)[] Categories =
        [
            ("سبائك",        "Ingots"),
            ("عملات",        "Coins"),
            ("مقتنيات فاخرة", "Premium Collectibles"),
        ];
    }
}
