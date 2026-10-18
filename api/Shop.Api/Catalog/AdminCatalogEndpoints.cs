using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Catalog;

public static class AdminCatalogEndpoints
{
    public static IEndpointRouteBuilder MapAdminCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AdminAuthorization.Policy);

        MapCategories(group.MapGroup("/categories"));
        MapProducts(group.MapGroup("/products"));

        return app;
    }

    private static void MapCategories(RouteGroupBuilder group)
    {
        group.MapPost("/", async (CategoryRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            var errors = CatalogValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var name = request.Name!.Trim();
            if (await NameTakenAsync(db, name, exceptId: null, ct))
            {
                return Conflict("A category with this name already exists.");
            }

            var category = new Category
            {
                Name = name,
                Slug = await Slugs.UniqueAsync(name, slug => db.Categories.AnyAsync(c => c.Slug == slug, ct)),
                SortOrder = request.SortOrder ?? 0
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/categories/{category.Id}", CatalogQueries.ToDto(category));
        });

        // The slug is kept on rename so shared links keep working.
        group.MapPut("/{id:int}", async (int id, CategoryRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            var errors = CatalogValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var category = await db.Categories.FindAsync([id], ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            var name = request.Name!.Trim();
            if (await NameTakenAsync(db, name, exceptId: id, ct))
            {
                return Conflict("A category with this name already exists.");
            }

            category.Name = name;
            category.SortOrder = request.SortOrder ?? category.SortOrder;
            await db.SaveChangesAsync(ct);

            return Results.Ok(CatalogQueries.ToDto(category));
        });

        group.MapDelete("/{id:int}", async (int id, ShopDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories.FindAsync([id], ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            {
                return Conflict("This category still has products. Move or deactivate them first.");
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }

    private static void MapProducts(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            ShopDbContext db,
            string? category,
            string? q,
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
                db, includeInactive: true, category, q, sort, pageNumber, size, ct));
        });

        group.MapGet("/{id:int}", async (int id, ShopDbContext db, CancellationToken ct) =>
        {
            var product = await LoadAsync(db, id, ct);
            return product is null ? Results.NotFound() : Results.Ok(CatalogQueries.ToDetail(product));
        });

        group.MapPost("/", async (ProductRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            var errors = await ValidateAsync(db, request, ct);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var product = new Product
            {
                Slug = await Slugs.UniqueAsync(request.Name!.Trim(), slug => db.Products.AnyAsync(p => p.Slug == slug, ct)),
                CreatedAt = DateTime.UtcNow
            };
            Apply(product, request);
            db.Products.Add(product);
            await db.SaveChangesAsync(ct);

            var created = await LoadAsync(db, product.Id, ct);
            return Results.Created($"/api/admin/products/{product.Id}", CatalogQueries.ToDetail(created!));
        });

        // The slug is kept on rename so shared links keep working.
        group.MapPut("/{id:int}", async (int id, ProductRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            var errors = await ValidateAsync(db, request, ct);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var product = await db.Products.FindAsync([id], ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            Apply(product, request);
            await db.SaveChangesAsync(ct);

            var updated = await LoadAsync(db, id, ct);
            return Results.Ok(CatalogQueries.ToDetail(updated!));
        });

        // Deactivate rather than delete: order lines keep pointing at the product.
        group.MapDelete("/{id:int}", async (int id, ShopDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync([id], ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            product.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }

    private static async Task<Dictionary<string, string[]>> ValidateAsync(
        ShopDbContext db, ProductRequest request, CancellationToken ct)
    {
        var errors = CatalogValidation.Validate(request);
        if (errors.Count == 0 && !await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            errors["categoryId"] = ["Category does not exist."];
        }

        return errors;
    }

    private static void Apply(Product product, ProductRequest request)
    {
        product.Name = request.Name!.Trim();
        product.CategoryId = request.CategoryId!.Value;
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price!.Value;
        product.IsActive = request.IsActive ?? false;

        product.Sizes.Clear();
        foreach (var size in CatalogValidation.NormalizeSizes(request.Sizes))
        {
            product.Sizes.Add(size);
        }
    }

    private static Task<Product?> LoadAsync(ShopDbContext db, int id, CancellationToken ct) =>
        db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    private static Task<bool> NameTakenAsync(ShopDbContext db, string name, int? exceptId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Id != exceptId && c.Name.ToLower() == name.ToLower(), ct);

    private static IResult Conflict(string detail) =>
        Results.Problem(detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");
}
