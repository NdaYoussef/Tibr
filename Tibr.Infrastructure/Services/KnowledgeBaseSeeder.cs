using Tibr.Application.Services.AiChatServices;

namespace Tibr.Infrastructure.Services
{
    public static class KnowledgeBaseSeeder
    {
        public static List<FaqEntry> FaqEntries() =>
        [
            new("f1", "What is fractional gold investment?",
                "Fractional gold investment lets you buy a fraction of a gram of gold, "
                + "making gold accessible at any budget rather than requiring full gram purchases.",
                "الاستثمار الجزئي في الذهب يتيح لك شراء كسور من الجرام، مما يجعل الذهب متاحاً لأي ميزانية دون الحاجة لشراء جرام كامل.",
                "ما هو الاستثمار الجزئي في الذهب؟"),
            new("f2", "Is my gold physically stored?",
                "Yes. Tibr stores your gold in certified vaults. You own a verified fraction "
                + "of physical gold, not just a digital number.",
                "نعم. تخزن تبر ذهبك في خزائن معتمدة. أنت تملك جزءاً مادياً موثقاً من الذهب، وليس مجرد رقم رقمي.",
                "هل الذهب مخزن فعلياً؟"),
            new("f3", "How do I sell my gold on Tibr?",
                "You can sell any of your gold fractions at any time through the app. "
                + "The current market price is used and funds are credited to your wallet.",
                "يمكنك بيع أي كمية من الذهب في أي وقت عبر التطبيق. يُستخدم سعر السوق الحالي وتُضاف الأموال إلى محفظتك.",
                "كيف أبيع ذهبي في تبر؟"),
            new("f4", "What is a gold fraction?",
                "A gold fraction is any amount of gold below one gram, such as 0.1g or 0.25g. "
                + "Tibr tracks ownership down to four decimal places.",
                "الجزء من الذهب هو أي كمية أقل من جرام واحد، مثل 0.1 أو 0.25 جرام. تبر تتبع الملكية لأربع خانات عشرية.",
                "ما هو الجزء من الذهب؟"),
            new("f5", "Is Tibr compliant with Islamic finance principles?",
                "Tibr's model is designed to align with Islamic finance — ownership is real, "
                + "immediate, and asset-backed. Consult a scholar for your specific situation.",
                "نموذج تبر مصمم ليتوافق مع التمويل الإسلامي — الملكية حقيقية وفورية ومدعومة بأصول مادية. يُرجى استشارة شيخ لحالتك الخاصة.",
                "هل تبر متوافقة مع أحكام الشريعة الإسلامية؟"),
            new("f6", "How does Tibr make money?",
                "Tibr earns a small commission on each transaction. "
                + "Premium agentic features are available via subscription.",
                "تبر تحصل على عمولة صغيرة على كل عملية بيع أو شراء. الميزات المتقدمة متاحة عبر اشتراك شهري.",
                "كيف تربح تبر؟"),
            new("f7", "How do I deposit money into Tibr?",
                "You can deposit funds via bank transfer or through the integrated payment gateway. "
                + "Once approved, the amount is credited to your cash wallet and ready to use.",
                "يمكنك إيداع الأموال عبر التحويل البنكي أو بوابة الدفع المدمجة. بعد الموافقة، تُضاف المبلغ إلى محفظتك النقدية وتكون جاهزة للاستخدام.",
                "كيف أودع أموالاً في تبر؟"),
            new("f8", "What products can I buy on Tibr?",
                "Tibr offers gold bars (1g, 5g, 10g, 20g, 50g, 100g), gold coins (Eagle, Maple Leaf), "
                + "silver bars (10g to 1kg), and silver coins. You can also buy fractional gold/silver starting from 0.01g.",
                "تبر تقدم سبائك ذهب (1، 5، 10، 20، 50، 100 جرام)، عملات ذهبية، سبائك فضة (10 جرام حتى 1 كجم)، وعملات فضة. كما يمكنك شراء ذهب وفضة بشكل جزئي starting من 0.01 جرام.",
                "ما هي المنتجات التي يمكنني شراؤها من تبر؟"),
            new("f9", "How do I get physical delivery of my gold or silver?",
                "You can request physical delivery of your products through the delivery page. "
                + "Processing times and shipping fees vary by product and your location.",
                "يمكنك طلب التوصيل الفيزيائي لمنتجاتك عبر صفحة التوصيل. مدة المعالجة ورسوم الشحن تختلف حسب المنتج وموقعك.",
                "كيف أحصل على التوصيل الفيزيائي للذهب أو الفضة؟"),
            new("f10", "What is a strategy or conditional order?",
                "A strategy order lets you set conditions to automatically buy or sell when gold or silver "
                + "reaches a target price. You can choose to be alerted only, or auto-execute the trade.",
                "الأمر الاستراتيجي يسمح لك بتحديد شروط للشراء أو البيع تلقائياً عندما يصل الذهب أو الفضة لسعر مستهدف. يمكنك اختيار التنبيه فقط أو التنفيذ التلقائي.",
                "ما هو الأمر الاستراتيجي أو الشرطي؟"),
            new("f11", "Is there a minimum withdrawal amount?",
                "You can withdraw any available balance from your cash wallet to your bank account. "
                + "Withdrawals are typically processed within 2 business days.",
                "يمكنك سحب أي رصيد متاح من محفظتك النقدية إلى حسابك البنكي. تتم معالجة السحوبات عادةً خلال يومي عمل.",
                "هل يوجد حد أدنى للسحب؟"),
            new("f12", "What features does Tibr offer?",
                "Tibr offers fractional gold and silver investment (from 0.01g), physical bars and coins, "
                + "live market prices, a savings planner, conditional/strategy orders, cart checkout, "
                + "favorites/wishlist, product reviews, physical delivery across Egypt, "
                + "a cash/gold/silver wallet, deposits via bank transfer or payment gateway, "
                + "withdrawals to bank account, support tickets, KYC verification, "
                + "profile and address management, and secure JWT authentication.",
                "تبر توفر الاستثمار الجزئي في الذهب والفضة (من 0.01 جرام)، السبائك والعملات الفيزيائية، "
                + "أسعار السوق المباشرة، مخطط ادخار، أوامر شرطية/استراتيجية، سلة تسوق، "
                + "المفضلة، تقييمات المنتجات، التوصيل الفيزيائي في جميع أنحاء مصر، "
                + "محفظة نقدي/ذهب/فضة، إيداع عبر التحويل البنكي أو بوابة الدفع، "
                + "سحب إلى الحساب البنكي، تذاكر الدعم، التحقق من الهوية (KYC)، "
                + "إدارة الملف الشخصي والعناوين، والمصادقة الآمنة عبر JWT.",
                "ما هي ميزات تبر؟"),
        ];

        public static List<FactEntry> FactEntries() =>
        [
            new("fc1", "Tibr's transaction commission is 0.8% per buy or sell order.",
                "عمولة تبر هي 0.8% على كل عملية شراء أو بيع."),
            new("fc2", "The minimum purchase unit is 0.01 grams of gold.",
                "أصغر وحدة شراء هي 0.01 جرام من الذهب."),
            new("fc3", "Withdrawals to a bank account are processed within 2 business days.",
                "معالجة السحوبات إلى الحساب البنكي تستغرق يومي عمل."),
            new("fc4", "Tibr supports both gold and silver. You can buy, sell, and trade both metals.",
                "تبر تدعم الذهب والفضة. يمكنك شراء وبيع المتاجرة بكلا المعدنين."),
            new("fc5", "There is no monthly fee for a basic Tibr account.",
                "لا توجد رسوم شهرية للحساب الأساسي في تبر."),
            new("fc6", "KYC (identity verification) is required before you can buy or sell on Tibr.",
                "التحقق من الهوية (KYC) مطلوب قبل أن تتمكن من الشراء أو البيع في تبر."),
            new("fc7", "Physical delivery of gold and silver products is available. Fees and times vary by product and location.",
                "التوصيل الفيزيائي لمنتجات الذهب والفضة متاح. الرسوم والمدة تختلف حسب المنتج والموقع."),
            new("fc8", "Tibr offers both fractional investment (from 0.01g) and physical products like bars and coins.",
                "تبر توفر الاستثمار الجزئي (من 0.01 جرام) والمنتجات المادية مثل السبائك والعملات."),
            new("fc9", "Gold and silver prices on Tibr are sourced from live market APIs and updated in real-time.",
                "أسعار الذهب والفضة في تبر مُشتقة من واجهات السوق المباشرة ويتم تحديثها آنياً."),
            new("fc10", "You can set conditional/strategy orders to buy or sell automatically when the price reaches a target.",
                "يمكنك إعداد أوامر شرطية/استراتيجية للشراء أو البيع تلقائياً عند وصول السعر للسعر المستهدف."),
            new("fc11", "Your Tibr wallet has three balances: Cash (EGP), Gold (grams), and Silver (grams).",
                "محفظة تبر تحتوي على ثلاث أرصدة: نقدي (جنيه)، ذهب (جرام)، وفضة (جرام)."),
            new("fc12", "You can add gold and silver products to your cart and checkout later. Cart items persist until you complete or cancel the order.",
                "يمكنك إضافة منتجات الذهب والفضة إلى سلة التسوق والشراء لاحقاً. تبقى العناصر في السلة حتى إتمام الطلب أو إلغائه."),
            new("fc13", "You can save products to your favorites/wishlist for quick access later.",
                "يمكنك حفظ المنتجات في المفضلة للوصول السريع إليها لاحقاً."),
            new("fc14", "After purchasing a product, you can leave a review with a rating and comment.",
                "بعد شراء منتج، يمكنك ترك تقييم مع تصنيف وتعليق."),
            new("fc15", "You can create support tickets for any issues or inquiries. A support agent will respond to you.",
                "يمكنك إنشاء تذاكر دعم لأي مشكلة أو استفسار. سيقوم أحد ممثلي الدعم بالرد عليك."),
            new("fc16", "Physical delivery of gold and silver is available across Egypt. Delivery fees and times vary by product and location.",
                "التوصيل الفيزيائي للذهب والفضة متاح في جميع أنحاء مصر. رسوم ومدة التوصيل تختلف حسب المنتج والموقع."),
            new("fc17", "You can deposit funds into your Tibr cash wallet via bank transfer or the integrated online payment gateway.",
                "يمكنك إيداع الأموال في محفظتك النقدية عبر التحويل البنكي أو بوابة الدفع الإلكتروني المدمجة."),
            new("fc18", "You can manage your profile information and saved addresses from your account settings.",
                "يمكنك إدارة معلومات ملفك الشخصي والعناوين المحفوظة من إعدادات الحساب."),
            new("fc19", "Tibr uses secure JWT-based authentication. All API requests require a valid access token.",
                "تبر تستخدم المصادقة الآمنة القائمة على JWT. جميع طلبات API تتطلب رمز وصول صالح."),
            new("fc20", "You can view your complete transaction history including buys, sells, deposits, and withdrawals.",
                "يمكنك عرض سجل المعاملات الكامل شامل عمليات الشراء والبيع والإيداع والسحب."),
        ];
    }
}
