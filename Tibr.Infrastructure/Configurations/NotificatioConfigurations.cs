using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tibr.Domain.Entities;

namespace Tibr.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(n => n.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.ActionUrl)
                .HasMaxLength(500);

            builder.HasOne(n => n.Admin)
                .WithMany()
                .HasForeignKey(n => n.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.AdminId, n.IsRead });
        }
    }
}