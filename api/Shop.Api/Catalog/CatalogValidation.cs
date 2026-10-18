namespace Shop.Api.Catalog;

internal static class CatalogValidation
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;
    private const decimal MaxPrice = 9_999_999_999.99m;
    private const int MaxSizes = 10;
    private const int MaxSizeLength = 20;

    public static readonly string[] SortOptions = ["newest", "price_asc", "price_desc"];

    public static Dictionary<string, string[]> ValidateListQuery(string? sort, int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (sort is not null && !SortOptions.Contains(sort))
        {
            errors["sort"] = [$"Sort must be one of: {string.Join(", ", SortOptions)}."];
        }

        if (page < 1)
        {
            errors["page"] = ["Page must be 1 or greater."];
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaxPageSize}."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(CategoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
        {
            errors["name"] = ["Name is required and must be at most 100 characters."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(ProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 200)
        {
            errors["name"] = ["Name is required and must be at most 200 characters."];
        }

        if (request.CategoryId is null)
        {
            errors["categoryId"] = ["Category is required."];
        }

        if ((request.Description?.Length ?? 0) > 4000)
        {
            errors["description"] = ["Description must be at most 4000 characters."];
        }

        if (request.Price is null or <= 0 or > MaxPrice || request.Price != decimal.Round(request.Price.Value, 2))
        {
            errors["price"] = [$"Price must be greater than 0, at most {MaxPrice}, with at most 2 decimal places."];
        }

        var sizes = NormalizeSizes(request.Sizes);
        if (sizes.Count > MaxSizes || sizes.Any(size => size.Length > MaxSizeLength))
        {
            errors["sizes"] = [$"At most {MaxSizes} sizes, each at most {MaxSizeLength} characters."];
        }

        return errors;
    }

    /// <summary>
    /// Trims sizes, drops blanks and removes case-insensitive duplicates while keeping the given order.
    /// </summary>
    public static List<string> NormalizeSizes(IEnumerable<string>? sizes) =>
        (sizes ?? [])
            .Select(size => size?.Trim() ?? string.Empty)
            .Where(size => size.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
