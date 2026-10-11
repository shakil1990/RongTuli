using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Api.Domain;

namespace Shop.Api.Data.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImage");

        builder.Property(x => x.Url).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.PublicId).HasMaxLength(500).IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Not unique: reordering swaps SortOrder values, which would trip a unique constraint mid-update.
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
    }
}
