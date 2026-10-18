namespace Shop.Api.Catalog;

public sealed record CategoryDto(int Id, string Name, string Slug, int SortOrder);

public sealed record ProductSummaryDto(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    string CategorySlug,
    string? CoverImageUrl,
    DateTime CreatedAt);

public sealed record ProductImageDto(int Id, string Url, int SortOrder);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<string> Sizes,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<ProductImageDto> Images);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record CategoryRequest(string? Name, int? SortOrder);

public sealed record ProductRequest(
    string? Name,
    int? CategoryId,
    string? Description,
    decimal? Price,
    IReadOnlyList<string>? Sizes,
    bool? IsActive);
