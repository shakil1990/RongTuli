namespace Shop.Api.Domain;

public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public int SortOrder { get; set; }

    public ICollection<Product> Products { get; } = new List<Product>();
}
