using Microsoft.EntityFrameworkCore;

namespace Tibr.Infrastructure.Seed
{
    public class DatabaseCleaner
    {
        private readonly DbContext _context;

        public DatabaseCleaner(DbContext context)
        {
            _context = context;
        }

        public async Task ClearAllAsync()
        {
            var tables = new[]
            {
                // Tier 4 — leaves
                "WalletTransactions",
                "Transactions",
                "Reviews",
                "Payments",
                "OrderItems",
                // Tier 3
                "DeliveryRequests",
                "Reservations",
                "Alerts",
                "Trades",
                "OrderConditions",
                "ChatOrderProposals",
                "ChatMessages",
                "AuditLogs",
                "Tickets",
                "Favorites",
                "CartItems",
                // Tier 2
                "OrdersInvestments",
                "Withdraws",
                "Deposits",
                "ChatConversations",
                "Plans",
                "Supports",
                "Notifications",
                "KYCDocuments",
                "Addresses",
                "Carts",
                "Wallets",
                "Products",
                // Tier 1 — roots
                "PriceSnapshots",
                "AssetPrices",
                "Categories",
                "Admins",
                "Users",
            };

            foreach (var table in tables)
            {
#pragma warning disable EF1002
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
#pragma warning restore EF1002
            }

            await _context.SaveChangesAsync();
        }
    }
}
