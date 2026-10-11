namespace Shop.Api.Domain;

public sealed class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Url { get; set; } = null!;

    /// <summary>
    /// Cloudinary public ID, kept so the asset can be deleted along with the row.
    /// </summary>
    public string PublicId { get; set; } = null!;

    /// <summary>
    /// Display order; 0 is the cover image.
    /// </summary>
    public int SortOrder { get; set; }
}
