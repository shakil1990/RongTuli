using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;

namespace Shop.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/categories", async (ShopDbContext db, CancellationToken ct) =>
            await db.Categories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.SortOrder))
                .ToListAsync(ct));

        group.MapGet("/products", async (
            ShopDbContext db,
            string? category,
            string? sort,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var pageNumber = page ?? 1;
            var size = pageSize ?? CatalogValidation.DefaultPageSize;
            var errors = CatalogValidation.ValidateListQuery(sort, pageNumber, size);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            return Results.Ok(await CatalogQueries.ListProductsAsync(
                db, includeInactive: false, category, search: null, sort, pageNumber, size, ct));
        });

        group.MapGet("/products/{slug}", async (string slug, ShopDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, ct);

            return product is null ? Results.NotFound() : Results.Ok(CatalogQueries.ToDetail(product));
        });

        return app;
    }
}
