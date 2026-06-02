using Microsoft.EntityFrameworkCore;
using Tibr.Domain.Entities;

namespace Tibr.Infrastructure.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // User and Admin entities
        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }

        // Product and Category entities
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        // KYC Document entity
        public DbSet<KYCDocument> KYCDocuments { get; set; }

        // User-related entities
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Cart and Cart Items entities
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Order and Order Items entities
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Payment entity
        public DbSet<Payment> Payments { get; set; }

        // Support Ticket entities
        public DbSet<Support> SupportTickets { get; set; }
        public DbSet<Ticket> TicketReplies { get; set; }

        // Audit Log entity
        public DbSet<AuditLog> AuditLogs { get; set; }


        // Investment & Wallet
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<AssetPrice> AssetPrices { get; set; }

        public DbSet<OrdersInvestment> OrdersInvestments { get; set; }
        public DbSet<OrderCondition> OrderConditions { get; set; }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Alert> Alerts { get; set; }
        public DbSet<DeliveryRequest> DeliveryRequests { get; set; }
        public DbSet<Address> Addresses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure foreign key relationships
            modelBuilder
                .Entity<KYCDocument>()
                .HasOne(k => k.User)
                .WithMany(u => u.KYCDocuments)
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<KYCDocument>()
                .HasOne(k => k.ReviewedByAdmin)
                .WithMany(a => a.ReviewedDocuments)
                .HasForeignKey(k => k.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Favorite>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Support>()
                .HasOne(st => st.User)
                .WithMany(u => u.SupportTickets)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Ticket>()
                .HasOne(tr => tr.Support)
                .WithMany(st => st.Tickets)
                .HasForeignKey(tr => tr.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<Ticket>()
                .HasOne(tr => tr.Admin)
                .WithMany(a => a.TicketReplies)
                .HasForeignKey(tr => tr.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder
                .Entity<AuditLog>()
                .HasOne(al => al.Admin)
                .WithMany(a => a.AuditLogs)
                .HasForeignKey(al => al.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision configuration to avoid SQL truncation warnings
            modelBuilder.Entity<CartItem>().Property(ci => ci.UnitPrice).HasPrecision(18, 2);

            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);

            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<Product>().Property(p => p.BuyPrice).HasPrecision(18, 2);

            modelBuilder.Entity<Product>().Property(p => p.SellPrice).HasPrecision(18, 2);

            modelBuilder.Entity<Product>().Property(p => p.Weight).HasPrecision(18, 3);

            modelBuilder.Entity<Product>().Property(p => p.Purity).HasPrecision(10, 4);
            modelBuilder.Entity<Product>().Property(p => p.Stock).HasPrecision(18, 4);
            modelBuilder.Entity<Product>().Property(p => p.MetalType)
                .HasConversion<string>()
                .HasMaxLength(20);
            modelBuilder.Entity<Product>().Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            #region Investment Module

            modelBuilder.Entity<Wallet>()
                .HasOne<User>()
                .WithMany(u => u.Wallets)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Deposit>()
                .HasOne<User>()
                .WithMany(u => u.Deposits)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrdersInvestment>()
                .HasOne<User>()
                .WithMany(u => u.InvestmentOrders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderCondition>()
                .HasOne(oc => oc.Order)
                .WithMany(o => o.Conditions)
                .HasForeignKey(oc => oc.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Wallet)
                .WithMany()
                .HasForeignKey(r => r.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trade>()
                .HasOne(t => t.Order)
                .WithMany(o => o.Trades)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Trade)
                .WithMany()
                .HasForeignKey(t => t.TradeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Order)
                .WithMany()
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryRequest>()
                .HasOne(dr => dr.Address)
                .WithMany()
                .HasForeignKey(dr => dr.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryRequest>()
                .HasOne<User>()
                .WithMany(u => u.DeliveryRequests)
                .HasForeignKey(dr => dr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Address>()
                .HasOne<User>()
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion
            #region Investment Decimal Precision

            modelBuilder.Entity<Wallet>()
                .Property(x => x.Balance)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Wallet>()
                .Property(x => x.ReservedBalance)
                .HasPrecision(18, 4);

            modelBuilder.Entity<WalletTransaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Deposit>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AssetPrice>()
                .Property(x => x.BuyPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<AssetPrice>()
                .Property(x => x.SellPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OrdersInvestment>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OrdersInvestment>()
                .Property(x => x.RequestedPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OrdersInvestment>()
                .Property(x => x.CurrentPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OrderCondition>()
                .Property(x => x.TargetValue)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Reservation>()
                .Property(x => x.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Trade>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Trade>()
                .Property(x => x.ExecutedPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Trade>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DeliveryRequest>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 4);

            #endregion
        }
    }
}
