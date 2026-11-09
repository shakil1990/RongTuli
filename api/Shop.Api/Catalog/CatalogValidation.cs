using Shop.Api.Security;

namespace Shop.Api.Catalog;

internal static class CatalogValidation
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;
    private const int MaxSearchLength = 100;
    private const int MaxCategoryNameLength = 100;
    private const int MaxProductNameLength = 200;
    private const int MaxDescriptionLength = 4000;
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

    /// <summary>
    /// The product list's category and search filters. Neither can match anything past the length
    /// of the column it is compared with, so a longer value is refused before it reaches a query.
    /// </summary>
    public static void ValidateFilters(Dictionary<string, string[]> errors, string? category, string? search)
    {
        if ((category?.Length ?? 0) > Slugs.MaxCategorySlugLength)
        {
            errors["category"] = [$"Category must be at most {Slugs.MaxCategorySlugLength} characters."];
        }

        if ((search?.Trim().Length ?? 0) > MaxSearchLength)
        {
            errors["q"] = [$"Search must be at most {MaxSearchLength} characters."];
        }
    }

    public static Dictionary<string, string[]> Validate(CategoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > MaxCategoryNameLength || !TextInput.IsSingleLine(name))
        {
            errors["name"] = [$"Name is required and must be at most {MaxCategoryNameLength} characters on one line."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(ProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > MaxProductNameLength || !TextInput.IsSingleLine(name))
        {
            errors["name"] = [$"Name is required and must be at most {MaxProductNameLength} characters on one line."];
        }

        if (request.CategoryId is null)
        {
            errors["categoryId"] = ["Category is required."];
        }

        if ((request.Description?.Length ?? 0) > MaxDescriptionLength || !TextInput.IsMultiLine(request.Description))
        {
            errors["description"] =
                [$"Description must be at most {MaxDescriptionLength} characters, with no hidden control characters."];
        }

        if (request.Price is null or <= 0 or > MaxPrice || request.Price != decimal.Round(request.Price.Value, 2))
        {
            errors["price"] = [$"Price must be greater than 0, at most {MaxPrice}, with at most 2 decimal places."];
        }

        var sizes = NormalizeSizes(request.Sizes);
        if (sizes.Count > MaxSizes || sizes.Any(size => size.Length > MaxSizeLength || !TextInput.IsSingleLine(size)))
        {
            errors["sizes"] = [$"At most {MaxSizes} sizes, each at most {MaxSizeLength} characters on one line."];
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
