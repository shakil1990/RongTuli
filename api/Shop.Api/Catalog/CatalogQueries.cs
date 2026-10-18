using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Api.Catalog;

internal static class CatalogQueries
{
    public static async Task<PagedResult<ProductSummaryDto>> ListProductsAsync(
        ShopDbContext db,
        bool includeInactive,
        string? categorySlug,
        string? search,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(p => p.Category.Slug == categorySlug);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        // Id breaks ties so paging is stable when prices or timestamps match.
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.Category.Slug,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummaryDto>(items, page, pageSize, totalCount, (totalCount + pageSize - 1) / pageSize);
    }

    public static ProductDetailDto ToDetail(Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.Description,
        product.Price,
        product.CategoryId,
        product.Category.Name,
        product.Category.Slug,
        product.Sizes.ToList(),
        product.IsActive,
        product.CreatedAt,
        product.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.SortOrder)).ToList());

    public static CategoryDto ToDto(Category category) =>
        new(category.Id, category.Name, category.Slug, category.SortOrder);
}
