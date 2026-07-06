using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using Bogus;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;

namespace Tibr.Infrastructure.Seed
{
    public class MassDataSeeder
    {
        private readonly DbContext _context;

        public MassDataSeeder(DbContext context)
        {
            _context = context;
        }

        public async Task SeedAllAsync(int userCount = 500)
        {
            await _context.Database.MigrateAsync();

            Randomizer.Seed = new Random(42);
            var seedTime = DateTime.UtcNow;
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Test@123");

            var f = new Faker();

            var bulkConfig = new BulkConfig { PreserveInsertOrder = true, SetOutputIdentity = true };

            // ==========================================
            // PHASE 1: INDEPENDENT TIER 1 ENTITIES
            // ==========================================

            // 1. Users
            var userFaker = new Faker<User>()
                .RuleFor(u => u.FirstName, fk => fk.Name.FirstName())
                .RuleFor(u => u.LastName, fk => fk.Name.LastName())
                .RuleFor(u => u.Email, fk => fk.Internet.Email())
                .RuleFor(u => u.Phone, fk => fk.Phone.PhoneNumber("01########"))
                .RuleFor(u => u.Password, hashedPassword)
                .RuleFor(u => u.Status, "Active")
                .RuleFor(u => u.OtpVerified, true)
                .RuleFor(u => u.KycStatus, fk => fk.PickRandom("Pending", "Verified", "Rejected"))
                .RuleFor(u => u.CreatedAt, fk => fk.Date.Past(1))
                .RuleFor(u => u.UpdatedAt, (fk, u) => u.CreatedAt)
                .RuleFor(u => u.IsDeleted, false);

            var users = userFaker.Generate(userCount);

            // Spread all users evenly over the past 5 months
            var fiveMonthsAgo = seedTime.AddMonths(-5);
            var daySpan = (int)(seedTime - fiveMonthsAgo).TotalDays;
            for (int i = 0; i < users.Count; i++)
            {
                var date = fiveMonthsAgo.AddDays((double)i / users.Count * daySpan);
                users[i].CreatedAt = date;
                users[i].UpdatedAt = date;
            }

            // Known test user with predictable credentials (most recent)
            users.Add(new User
            {
                FirstName = "Eslam",
                LastName = "Legend",
                Email = "eslamlegend5@gmail.com",
                Phone = "010671082223",
                Password = hashedPassword,
                Status = "Active",
                OtpVerified = true,
                KycStatus = "Verified",
                CreatedAt = seedTime.AddMonths(-1), UpdatedAt = seedTime.AddMonths(-1), IsDeleted = false
            });

            Console.WriteLine("  eslamlegend5@gmail.com / Test@123");

            await _context.BulkInsertAsync(users, bulkConfig);

            // 2. Admins (no password — server-side only)
            var admins = new List<Admin>
            {
                new()
                {
                    Name = "Admin Tibr",
                    Email = "admin@tibr.com",
                    Status = "Active",
                    CreatedAt = seedTime, UpdatedAt = seedTime, IsDeleted = false
                }
            };
            await _context.BulkInsertAsync(admins, bulkConfig);

            // 3. Categories
            var categoryNames = new[] { "Bars", "Coins", "Rings", "Necklaces" };
            var categories = categoryNames.Select(name => new Category
            {
                Name = name,
                CreatedAt = seedTime,
                UpdatedAt = seedTime,
                IsDeleted = false
            }).ToList();
            await _context.BulkInsertAsync(categories, bulkConfig);

            // 19. AssetPrices
            var livePrices = new List<AssetPrice>
            {
                new() { AssetType = AssetType.Gold, BuyPrice = 3950.0000m, SellPrice = 4050.0000m, Source = "GoldAPI.io", CreatedAt = seedTime, UpdatedAt = seedTime },
                new() { AssetType = AssetType.Silver, BuyPrice = 48.5000m, SellPrice = 51.5000m, Source = "GoldAPI.io", CreatedAt = seedTime, UpdatedAt = seedTime }
            };
            await _context.BulkInsertAsync(livePrices, new BulkConfig { PreserveInsertOrder = true });

            // 20. PriceSnapshots
            var snapshots = new List<PriceSnapshot>();
            for (int i = 30; i >= 0; i--)
            {
                var snapshotDate = seedTime.AddDays(-i).Date;
                snapshots.Add(new PriceSnapshot { AssetType = AssetType.Gold, Price = Math.Round(f.Random.Decimal(3800m, 4200m), 4), SnapshotDate = snapshotDate, CreatedAt = seedTime, UpdatedAt = seedTime, IsDeleted = false });
                snapshots.Add(new PriceSnapshot { AssetType = AssetType.Silver, Price = Math.Round(f.Random.Decimal(45m, 55m), 4), SnapshotDate = snapshotDate, CreatedAt = seedTime, UpdatedAt = seedTime, IsDeleted = false });
            }
            await _context.BulkInsertAsync(snapshots, new BulkConfig { PreserveInsertOrder = true });

            // ==========================================
            // PHASE 2: DEPENDENT TIER 2 ENTITIES
            // ==========================================

            // 4. Products
            var productFaker = new Faker<Product>()
                .RuleFor(p => p.CategoryId, fk => fk.PickRandom(categories).Id)
                .RuleFor(p => p.Name, fk => $"{fk.Commerce.ProductAdjective()} {fk.PickRandom("Tibr", "Pharaonic", "Investment")} Piece")
                .RuleFor(p => p.MetalType, fk => fk.PickRandom<MetalType>())
                .RuleFor(p => p.Purity, fk => Math.Round(fk.Random.Decimal(0.8750m, 0.9999m), 4))
                .RuleFor(p => p.Weight, fk => Math.Round(fk.Random.Decimal(1m, 100m), 3))
                .RuleFor(p => p.BuyPrice, fk => Math.Round(fk.Random.Decimal(3000m, 4500m), 2))
                .RuleFor(p => p.SellPrice, (fk, p) => Math.Round(p.BuyPrice * 1.025m, 2))
                .RuleFor(p => p.Status, ProductStatus.Active)
                .RuleFor(p => p.Stock, fk => fk.Random.Long(100, 5000))
                .RuleFor(p => p.CreatedAt, seedTime)
                .RuleFor(p => p.UpdatedAt, seedTime)
                .RuleFor(p => p.IsDeleted, false);

            var products = productFaker.Generate(40);
            await _context.BulkInsertAsync(products, bulkConfig);

            // Collection builders for tier 2-4
            var wallets = new List<Wallet>();
            var addresses = new List<Address>();
            var carts = new List<Cart>();
            var kycDocuments = new List<KYCDocument>();
            var notifications = new List<Notification>();
            var supports = new List<Support>();
            var plans = new List<Plan>();
            var chatConversations = new List<ChatConversation>();
            var deposits = new List<Deposit>();
            var withdraws = new List<Withdraw>();
            var ordersInvestments = new List<OrdersInvestment>();

            foreach (var user in users)
            {
                var userTime = user.CreatedAt;

                // 16. Wallets (3 per user)
                wallets.Add(new Wallet { UserId = user.Id, WalletType = WalletType.Cash, Balance = Math.Round(f.Random.Decimal(5000m, 200000m), 4), ReservedBalance = 0m, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                wallets.Add(new Wallet { UserId = user.Id, WalletType = WalletType.Gold, Balance = Math.Round(f.Random.Decimal(0m, 75m), 4), ReservedBalance = 0m, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                wallets.Add(new Wallet { UserId = user.Id, WalletType = WalletType.Silver, Balance = Math.Round(f.Random.Decimal(0m, 1500m), 4), ReservedBalance = 0m, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });

                // 28. Addresses
                addresses.Add(new Address { UserId = user.Id, City = "Cairo", Area = f.Address.County(), Street = f.Address.StreetName(), Building = f.Address.BuildingNumber(), IsDefault = true, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });

                // 5. Carts
                carts.Add(new Cart { UserId = user.Id, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });

                // 14. KYCDocuments
                if (user.KycStatus != "Pending")
                {
                    long? reviewer = user.KycStatus == "Verified" ? f.PickRandom(admins).Id : null;
                    kycDocuments.Add(new KYCDocument { UserId = user.Id, DocumentType = f.PickRandom("NationalId", "Passport"), DocumentNumber = f.Random.Replace("##############"), DocumentFront = $"kyc_documents/{user.Id}/front.jpg", DocumentBack = $"kyc_documents/{user.Id}/back.jpg", SelfieImage = $"kyc_documents/{user.Id}/selfie.jpg", Status = user.KycStatus, ReviewedBy = reviewer, CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 15. Notifications
                if (f.Random.Bool(0.7f))
                {
                    notifications.Add(new Notification { UserId = user.Id, Title = "Welcome to Tibr!", Message = "Start investing in Gold and Silver safely.", IsRead = f.Random.Bool(), CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 11. Supports
                if (f.Random.Bool(0.2f))
                {
                    supports.Add(new Support { UserId = user.Id, Subject = f.PickRandom("Deposit Delay", "ID Verification Pending", "App Bug"), Status = f.PickRandom<Support.SupportStatus>(), CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 31. Plans
                if (f.Random.Bool(0.4f))
                {
                    var goal = f.PickRandom("reach_grams", "reach_value_egp", "monthly_budget");
                    var isBoth = f.Random.Bool(0.2f);
                    plans.Add(new Plan
                    {
                        UserId = user.Id,
                        GoalType = goal,
                        Asset = isBoth ? "both" : f.PickRandom("Gold", "Silver"),
                        TargetAmount = Math.Round(f.Random.Decimal(50m, 500m), 4),
                        TimeframeWeeks = goal == "monthly_budget" ? null : f.Random.Number(12, 52),
                        MonthlyBudgetEgp = goal == "monthly_budget" ? Math.Round(f.Random.Decimal(2000m, 10000m), 2) : null,
                        PriceAtCreation = 4050.00m,
                        SilverPriceAtCreation = isBoth ? 51.50m : null,
                        Status = PlanStatus.Active,
                        CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false
                    });
                }

                // 32. ChatConversations
                if (f.Random.Bool(0.5f))
                {
                    chatConversations.Add(new ChatConversation { UserId = user.Id, Title = f.Lorem.Sentence(3), IsActive = f.Random.Bool(0.8f), CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 18. Deposits
                if (f.Random.Bool(0.6f))
                {
                    deposits.Add(new Deposit { UserId = user.Id, Amount = Math.Round(f.Random.Decimal(3000m, 50000m), 2), Status = DepositStatus.Completed, PaymentMethod = f.PickRandom<PaymentMethod>(), TransactionRef = f.Random.AlphaNumeric(12).ToUpper(), CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 29. Withdraws
                if (f.Random.Bool(0.15f))
                {
                    withdraws.Add(new Withdraw { UserId = user.Id, Amount = Math.Round(f.Random.Decimal(1000m, 10000m), 2), Type = f.PickRandom<WithdrawType>(), Name = $"{user.FirstName} {user.LastName}", Number = f.Finance.Iban(), CreatedAt = userTime, UpdatedAt = userTime, IsDeleted = false });
                }

                // 21. OrdersInvestments (skip test user — custom history built later)
                if (user.Email != "eslamlegend5@gmail.com")
                {
                    var numOrders = f.Random.Number(0, 5);
                    for (int oi = 0; oi < numOrders; oi++)
                    {
                        var type = f.PickRandom<OrderType>();
                        var orderDate = f.Date.Between(userTime, seedTime);
                        ordersInvestments.Add(new OrdersInvestment
                        {
                            UserId = user.Id,
                            AssetType = f.PickRandom<AssetType>(),
                            OrderType = type,
                            ExecutionMode = f.PickRandom<ExecutionMode>(),
                            Quantity = type == OrderType.Sell ? Math.Round(f.Random.Decimal(1m, 10m), 4) : 0m,
                            RequestedPrice = Math.Round(f.Random.Decimal(3900m, 4100m), 4),
                            CurrentPrice = 4050.0000m,
                            Status = f.PickRandom<OrderStatus>(),
                            ExecutionType = f.PickRandom<ExecutionType>(),
                            MaxBudgetEgp = type == OrderType.Buy ? Math.Round(f.Random.Decimal(5000m, 20000m), 2) : null,
                            ExpiryDate = userTime.AddDays(30),
                            CreatedAt = orderDate, UpdatedAt = orderDate, IsDeleted = false
                        });
                    }
                }
            }

            // Tier 2 bulk insert
            await _context.BulkInsertAsync(wallets, bulkConfig);
            await _context.BulkInsertAsync(addresses, bulkConfig);
            await _context.BulkInsertAsync(carts, bulkConfig);
            await _context.BulkInsertAsync(kycDocuments, bulkConfig);
            await _context.BulkInsertAsync(notifications, bulkConfig);
            await _context.BulkInsertAsync(supports, bulkConfig);
            await _context.BulkInsertAsync(plans, bulkConfig);
            await _context.BulkInsertAsync(chatConversations, bulkConfig);
            await _context.BulkInsertAsync(deposits, bulkConfig);
            await _context.BulkInsertAsync(withdraws, bulkConfig);
            await _context.BulkInsertAsync(ordersInvestments, bulkConfig);

            // Lookup caches
            var userWalletMap = wallets.GroupBy(w => w.UserId).ToDictionary(g => g.Key, g => g.ToList());
            var userAddressMap = addresses.ToDictionary(a => a.UserId);

            // ==========================================
            // PHASE 3: TIER 3 DEPENDENT ENTITIES
            // ==========================================

            // 6. CartItems
            var cartItems = new List<CartItem>();
            foreach (var cart in carts)
            {
                if (f.Random.Bool(0.4f))
                {
                    var chosenProds = f.PickRandom(products, f.Random.Number(1, 2));
                    foreach (var prod in chosenProds)
                    {
                        cartItems.Add(new CartItem { CartId = cart.Id, ProductId = prod.Id, Quantity = f.Random.Number(1, 2), UnitPrice = prod.SellPrice, CreatedAt = cart.CreatedAt, UpdatedAt = cart.CreatedAt, IsDeleted = false });
                    }
                }
            }
            if (cartItems.Count != 0) await _context.BulkInsertAsync(cartItems, bulkConfig);

            // 7. Favorites
            var favorites = users.Where(_ => f.Random.Bool(0.3f))
                .Select(u => new Favorite { UserId = u.Id, ProductId = f.PickRandom(products).Id, CreatedAt = u.CreatedAt, UpdatedAt = u.CreatedAt, IsDeleted = false }).ToList();
            if (favorites.Count != 0) await _context.BulkInsertAsync(favorites, bulkConfig);

            // 12. Tickets
            var tickets = supports.Select(s => new Ticket { AdminId = f.PickRandom(admins).Id, SupportId = s.Id, Message = f.Lorem.Paragraph(2), CreatedAt = s.CreatedAt, UpdatedAt = s.CreatedAt, IsDeleted = false }).ToList();
            if (tickets.Count != 0) await _context.BulkInsertAsync(tickets, bulkConfig);

            // 13. AuditLogs
            var auditLogs = admins.Select(a => new AuditLog { AdminId = a.Id, Action = f.PickRandom("UpdatedProduct", "ReviewedKYC"), TableName = f.PickRandom("Products", "KYCDocuments"), RecordId = f.Random.Long(1, 40), CreatedAt = seedTime, UpdatedAt = seedTime, IsDeleted = false }).ToList();
            await _context.BulkInsertAsync(auditLogs, bulkConfig);

            // 33. ChatMessages + 34. ChatOrderProposals
            var chatMessages = new List<ChatMessage>();
            var chatProposals = new List<ChatOrderProposal>();
            foreach (var conv in chatConversations)
            {
                chatMessages.Add(new ChatMessage { ConversationId = conv.Id, Role = ChatRole.User, Content = "I want to purchase some Gold bars.", CreatedAt = conv.CreatedAt, UpdatedAt = conv.CreatedAt, IsDeleted = false });
                var replyTime = conv.CreatedAt.AddSeconds(4);
                chatMessages.Add(new ChatMessage { ConversationId = conv.Id, Role = ChatRole.Assistant, Content = "Sure, here is a custom procurement proposal tailored for you.", CreatedAt = replyTime, UpdatedAt = replyTime, IsDeleted = false });

                if (f.Random.Bool(0.5f))
                {
                    chatProposals.Add(new ChatOrderProposal { ConversationId = conv.Id, ProposalJson = "{\"grams\":2.5, \"metal\":\"Gold\", \"priceEgp\":10125}", Status = ProposalStatus.Pending, ExpiresAt = replyTime.AddHours(2), CreatedAt = replyTime, UpdatedAt = replyTime, IsDeleted = false });
                }
            }
            if (chatMessages.Count != 0) await _context.BulkInsertAsync(chatMessages, bulkConfig);
            if (chatProposals.Count != 0) await _context.BulkInsertAsync(chatProposals, bulkConfig);

            // 22. OrderConditions
            var orderConditions = ordersInvestments.Where(o => o.ExecutionMode == ExecutionMode.Strategy)
                .Select(o => new OrderCondition { OrderId = o.Id, ConditionType = ConditionType.PriceTarget, Operator = f.PickRandom<ConditionOperator>(), TargetValue = o.RequestedPrice, CreatedAt = o.CreatedAt, UpdatedAt = o.CreatedAt, IsDeleted = false }).ToList();
            if (orderConditions.Count != 0) await _context.BulkInsertAsync(orderConditions, bulkConfig);

            // 24. Trades
            var trades = ordersInvestments.Where(o => o.Status == OrderStatus.Executed)
                .Select(o =>
                {
                    var qty = o.Quantity > 0 ? o.Quantity : Math.Round((o.MaxBudgetEgp ?? 10000m) / o.RequestedPrice, 4);
                    var isBuy = o.OrderType == OrderType.Buy;
                    return new Trade
                    {
                        OrderId = o.Id,
                        UserId = o.UserId,
                        AssetType = o.AssetType,
                        Side = isBuy ? TradeSide.Buy : TradeSide.Sell,
                        Quantity = qty,
                        RemainingQuantity = isBuy ? qty : 0,
                        ExecutedPrice = o.RequestedPrice,
                        TotalAmount = Math.Round(qty * o.RequestedPrice, 2),
                        ExecutedAt = o.CreatedAt,
                        CreatedAt = o.CreatedAt, UpdatedAt = o.CreatedAt, IsDeleted = false
                    };
                }).ToList();
            if (trades.Count != 0) await _context.BulkInsertAsync(trades, bulkConfig);

            // 26. Alerts
            var alerts = ordersInvestments.Where(o => o.Status is OrderStatus.Triggered or OrderStatus.Executed)
                .Select(o => new Alert { UserId = o.UserId, OrderId = o.Id, AlertType = AlertType.PriceReached, Message = $"Your target price of {o.RequestedPrice} was hit!", SentAt = o.CreatedAt, CreatedAt = o.CreatedAt, UpdatedAt = o.CreatedAt, IsDeleted = false }).ToList();
            if (alerts.Count != 0) await _context.BulkInsertAsync(alerts, bulkConfig);

            // 23. Reservations
            var reservations = new List<Reservation>();
            foreach (var o in ordersInvestments.Where(o => o.Status == OrderStatus.Pending))
            {
                var targetType = o.OrderType == OrderType.Buy ? WalletType.Cash : (o.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver);
                var wallet = userWalletMap[o.UserId].First(w => w.WalletType == targetType);
                var resAmount = o.OrderType == OrderType.Buy ? (o.MaxBudgetEgp ?? 5000m) : o.Quantity;

                reservations.Add(new Reservation { UserId = o.UserId, WalletId = wallet.Id, OrderId = o.Id, Amount = resAmount, Status = ReservationStatus.Active, CreatedAt = o.CreatedAt, UpdatedAt = o.CreatedAt, IsDeleted = false });
            }
            if (reservations.Count != 0) await _context.BulkInsertAsync(reservations, bulkConfig);

            // 27. DeliveryRequests
            var deliveryRequests = users.Where(_ => f.Random.Bool(0.15f))
                .Select(u => new DeliveryRequest
                {
                    UserId = u.Id,
                    AssetType = f.PickRandom<AssetType>(),
                    Quantity = Math.Round(f.Random.Decimal(5m, 50m), 4),
                    AddressId = userAddressMap[u.Id].Id,
                    Status = f.PickRandom<DeliveryStatus>(),
                    TrackingNumber = f.Random.Bool(0.6f) ? $"TRK-{f.Random.Number(100000, 999999)}" : null,
                    CreatedAt = u.CreatedAt, UpdatedAt = u.CreatedAt, IsDeleted = false
                }).ToList();
            if (deliveryRequests.Count != 0) await _context.BulkInsertAsync(deliveryRequests, bulkConfig);

            // 8. Orders
            var orders = new List<Order>();
            foreach (var user in users)
            {
                if (f.Random.Bool(0.4f))
                {
                    orders.Add(new Order { UserId = user.Id, OrderNumber = $"ORD-{user.CreatedAt:yyyyMMdd}-{f.Random.AlphaNumeric(5).ToUpper()}", TotalAmount = 0m, PaymentStatus = f.PickRandom("Paid", "Pending"), OrderStatus = f.PickRandom("Delivered", "Confirmed"), CreatedAt = user.CreatedAt.AddDays(2), UpdatedAt = user.CreatedAt.AddDays(2), IsDeleted = false });
                }
            }
            await _context.BulkInsertAsync(orders, bulkConfig);

            // ==========================================
            // PHASE 4: TIER 4 DEPENDENT ENTITIES
            // ==========================================

            // 9. OrderItems
            var orderItems = new List<OrderItem>();
            foreach (var order in orders)
            {
                var itemsCount = f.Random.Number(1, 3);
                var selectedProducts = f.PickRandom(products, itemsCount);
                var totalOrderSum = 0m;

                foreach (var prod in selectedProducts)
                {
                    var qty = f.Random.Number(1, 2);
                    var itemTotal = prod.SellPrice * qty;
                    totalOrderSum += itemTotal;

                    orderItems.Add(new OrderItem { OrderId = order.Id, ProductId = prod.Id, Quantity = qty, Price = itemTotal, CreatedAt = order.CreatedAt, UpdatedAt = order.CreatedAt, IsDeleted = false });
                }
                order.TotalAmount = totalOrderSum;
            }
            await _context.BulkUpdateAsync(orders);
            await _context.BulkInsertAsync(orderItems, bulkConfig);

            // 10. Payments
            var payments = orders.Where(o => o.PaymentStatus == "Paid")
                .Select(o => new Payment { OrderId = o.Id, UserId = o.UserId, PaymentMethod = f.PickRandom("Paymob", "Visa"), Amount = o.TotalAmount, Status = "Success", PaidAt = o.CreatedAt, CreatedAt = o.CreatedAt, UpdatedAt = o.CreatedAt, IsDeleted = false }).ToList();
            if (payments.Count != 0) await _context.BulkInsertAsync(payments, bulkConfig);

            // 30. Reviews (unique on OrderId + UserId)
            var reviews = orders.Where(o => o.OrderStatus == "Delivered" && f.Random.Bool(0.6f))
                .Select(o => new Review { OrderId = o.Id, UserId = o.UserId, Description = f.Lorem.Sentence(6), Value = f.Random.Number(4, 5), CreatedAt = o.CreatedAt.AddDays(1), UpdatedAt = o.CreatedAt.AddDays(1), IsDeleted = false }).ToList();
            if (reviews.Count != 0) await _context.BulkInsertAsync(reviews, bulkConfig);

            // 25. Transactions
            var financialTransactions = trades.Select(t => new Transaction { UserId = t.UserId, TradeId = t.Id, TransactionType = t.Side == TradeSide.Buy ? TransactionType.Buy : TransactionType.Sell, Amount = t.TotalAmount, Status = TransactionStatusEnum.Success, CreatedAt = t.CreatedAt, UpdatedAt = t.CreatedAt, IsDeleted = false }).ToList();
            if (financialTransactions.Count != 0) await _context.BulkInsertAsync(financialTransactions, bulkConfig);

            // 17. WalletTransactions
            var walletTransactions = new List<WalletTransaction>();

            foreach (var dep in deposits)
            {
                var cashWallet = userWalletMap[dep.UserId].First(w => w.WalletType == WalletType.Cash);
                walletTransactions.Add(new WalletTransaction { WalletId = cashWallet.Id, Type = WalletTransactionType.Credit, Amount = dep.Amount, ReferenceType = ReferenceType.Deposit, ReferenceId = dep.Id, CreatedAt = dep.CreatedAt, UpdatedAt = dep.CreatedAt, IsDeleted = false });
            }

            foreach (var trade in trades)
            {
                var cashWallet = userWalletMap[trade.UserId].First(w => w.WalletType == WalletType.Cash);
                var assetType = trade.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                var assetWallet = userWalletMap[trade.UserId].First(w => w.WalletType == assetType);

                if (trade.Side == TradeSide.Buy)
                {
                    walletTransactions.Add(new WalletTransaction { WalletId = cashWallet.Id, Type = WalletTransactionType.Debit, Amount = trade.TotalAmount, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = trade.CreatedAt, UpdatedAt = trade.CreatedAt, IsDeleted = false });
                    walletTransactions.Add(new WalletTransaction { WalletId = assetWallet.Id, Type = WalletTransactionType.Credit, Amount = trade.Quantity, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = trade.CreatedAt, UpdatedAt = trade.CreatedAt, IsDeleted = false });
                }
                else
                {
                    walletTransactions.Add(new WalletTransaction { WalletId = cashWallet.Id, Type = WalletTransactionType.Credit, Amount = trade.TotalAmount, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = trade.CreatedAt, UpdatedAt = trade.CreatedAt, IsDeleted = false });
                    walletTransactions.Add(new WalletTransaction { WalletId = assetWallet.Id, Type = WalletTransactionType.Debit, Amount = trade.Quantity, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = trade.CreatedAt, UpdatedAt = trade.CreatedAt, IsDeleted = false });
                }
            }

            foreach (var del in deliveryRequests)
            {
                var assetType = del.AssetType == AssetType.Gold ? WalletType.Gold : WalletType.Silver;
                var assetWallet = userWalletMap[del.UserId].First(w => w.WalletType == assetType);
                walletTransactions.Add(new WalletTransaction { WalletId = assetWallet.Id, Type = WalletTransactionType.Debit, Amount = del.Quantity, ReferenceType = ReferenceType.Delivery, ReferenceId = del.Id, CreatedAt = del.CreatedAt, UpdatedAt = del.CreatedAt, IsDeleted = false });
            }

            if (walletTransactions.Count != 0) await _context.BulkInsertAsync(walletTransactions, bulkConfig);

            // ==========================================
            // TEST USER: 5-month trade history
            // ==========================================
            var testUser = users.FirstOrDefault(u => u.Email == "eslamlegend5@gmail.com");
            if (testUser is not null)
            {
                var cashW = userWalletMap[testUser.Id].First(w => w.WalletType == WalletType.Cash);
                var goldW = userWalletMap[testUser.Id].First(w => w.WalletType == WalletType.Gold);

                cashW.Balance = 100000m;
                cashW.ReservedBalance = 0m;
                goldW.Balance = 0m;
                goldW.ReservedBalance = 0m;
                await _context.BulkUpdateAsync(new List<Wallet> { cashW, goldW }, bulkConfig);

                // Price curve: gold varied ~3600–4200 over 5 months
                // Cheapest-first sell logic: sell 4g deducts from 3600 buy first, then 3900
                var historyEvents = new[]
                {
                    new { MonthsAgo = 5, Side = TradeSide.Buy,  Qty = 10m, Price = 3600m },
                    new { MonthsAgo = 4, Side = TradeSide.Buy,  Qty = 5m,  Price = 3900m },
                    new { MonthsAgo = 3, Side = TradeSide.Buy,  Qty = 8m,  Price = 4200m },
                    new { MonthsAgo = 2, Side = TradeSide.Sell, Qty = 4m,  Price = 4000m },
                    new { MonthsAgo = 1, Side = TradeSide.Buy,  Qty = 3m,  Price = 3800m },
                };

                // Insert orders first to populate their IDs
                var testOrders = new List<OrdersInvestment>();
                foreach (var ev in historyEvents)
                {
                    var ts = seedTime.AddMonths(-ev.MonthsAgo);
                    var total = Math.Round(ev.Qty * ev.Price, 2);
                    var isBuy = ev.Side == TradeSide.Buy;
                    testOrders.Add(new OrdersInvestment
                    {
                        UserId = testUser.Id,
                        AssetType = AssetType.Gold,
                        OrderType = isBuy ? OrderType.Buy : OrderType.Sell,
                        ExecutionMode = ExecutionMode.Direct,
                        ExecutionType = ExecutionType.AutoExecute,
                        Quantity = isBuy ? 0 : ev.Qty,
                        MaxBudgetEgp = isBuy ? total : null,
                        RequestedPrice = ev.Price,
                        CurrentPrice = ev.Price,
                        Status = OrderStatus.Executed,
                        CreatedAt = ts, UpdatedAt = ts, IsDeleted = false
                    });
                }
                await _context.BulkInsertAsync(testOrders, bulkConfig);

                // Build and insert trades using populated order IDs
                var testTrades = new List<Trade>();
                for (int ei = 0; ei < historyEvents.Length; ei++)
                {
                    var ev = historyEvents[ei];
                    var order = testOrders[ei];
                    var ts = seedTime.AddMonths(-ev.MonthsAgo);
                    var total = Math.Round(ev.Qty * ev.Price, 2);
                    var isBuy = ev.Side == TradeSide.Buy;
                    testTrades.Add(new Trade
                    {
                        OrderId = order.Id,
                        UserId = testUser.Id,
                        AssetType = AssetType.Gold,
                        Side = ev.Side,
                        Quantity = ev.Qty,
                        RemainingQuantity = isBuy ? ev.Qty : 0,
                        ExecutedPrice = ev.Price,
                        TotalAmount = total,
                        ExecutedAt = ts,
                        CreatedAt = ts, UpdatedAt = ts, IsDeleted = false
                    });
                }
                await _context.BulkInsertAsync(testTrades, bulkConfig);

                // Build transactions + wallet txns using populated trade IDs
                var testTransactions = new List<Transaction>();
                var testWalletTxns = new List<WalletTransaction>();

                for (int ei = 0; ei < historyEvents.Length; ei++)
                {
                    var ev = historyEvents[ei];
                    var trade = testTrades[ei];
                    var ts = seedTime.AddMonths(-ev.MonthsAgo);
                    var total = Math.Round(ev.Qty * ev.Price, 2);
                    var isBuy = ev.Side == TradeSide.Buy;

                    testTransactions.Add(new Transaction
                    {
                        UserId = testUser.Id,
                        TradeId = trade.Id,
                        TransactionType = isBuy ? TransactionType.Buy : TransactionType.Sell,
                        Amount = total,
                        Status = TransactionStatusEnum.Success,
                        CreatedAt = ts, UpdatedAt = ts, IsDeleted = false
                    });

                    if (isBuy)
                    {
                        goldW.Balance += ev.Qty;
                        cashW.Balance -= total;
                        testWalletTxns.Add(new WalletTransaction { WalletId = cashW.Id, Type = WalletTransactionType.Debit, Amount = total, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = ts, UpdatedAt = ts, IsDeleted = false });
                        testWalletTxns.Add(new WalletTransaction { WalletId = goldW.Id, Type = WalletTransactionType.Credit, Amount = ev.Qty, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = ts, UpdatedAt = ts, IsDeleted = false });
                    }
                    else
                    {
                        goldW.Balance -= ev.Qty;
                        cashW.Balance += total;
                        testWalletTxns.Add(new WalletTransaction { WalletId = cashW.Id, Type = WalletTransactionType.Credit, Amount = total, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = ts, UpdatedAt = ts, IsDeleted = false });
                        testWalletTxns.Add(new WalletTransaction { WalletId = goldW.Id, Type = WalletTransactionType.Debit, Amount = ev.Qty, ReferenceType = ReferenceType.Trade, ReferenceId = trade.Id, CreatedAt = ts, UpdatedAt = ts, IsDeleted = false });
                    }
                }

                await _context.BulkInsertAsync(testTransactions, bulkConfig);
                await _context.BulkInsertAsync(testWalletTxns, bulkConfig);

                await _context.BulkUpdateAsync(new List<Wallet> { cashW, goldW }, bulkConfig);
            }
        }
    }
}
