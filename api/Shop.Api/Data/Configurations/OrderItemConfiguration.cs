using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Api.Domain;

namespace Shop.Api.Data.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItem");

        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Size).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a product that has been ordered must never be hard-deleted; deactivate it instead.
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
    }
}
