using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Api.Domain;

namespace Shop.Api.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasData(
            new Category { Id = 1, Name = "Men", Slug = "men", SortOrder = 1 },
            new Category { Id = 2, Name = "Women", Slug = "women", SortOrder = 2 },
            new Category { Id = 3, Name = "Kids", Slug = "kids", SortOrder = 3 },
            new Category { Id = 4, Name = "Accessories", Slug = "accessories", SortOrder = 4 });
    }
}
