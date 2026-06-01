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
        }
    }

}
