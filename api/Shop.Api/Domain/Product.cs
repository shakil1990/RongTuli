namespace Shop.Api.Domain;

/// <summary>
/// A sellable item. Only active products are public; an active product must have 1 to 5 images.
/// </summary>
public sealed class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>
    /// Sizes on offer (for example S, M, L). Stock is not tracked per size.
    /// </summary>
    public ICollection<string> Sizes { get; private set; } = new List<string>();

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ProductImage> Images { get; } = new List<ProductImage>();
}
