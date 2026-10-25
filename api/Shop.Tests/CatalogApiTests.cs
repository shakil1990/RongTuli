using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Tests;

public sealed class CatalogApiTests : IDisposable
{
    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Categories_ReturnsSeededCategoriesInOrder()
    {
        var categories = await _factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        Assert.Equal(["men", "women", "kids", "accessories"], categories!.Select(c => c.Slug));
    }

    [Fact]
    public async Task Products_SortNewest_ReturnsLatestFirstAndHidesInactive()
    {
        await SeedAsync(
            Product("Old", 100, DateTime.UtcNow.AddDays(-3)),
            Product("New", 300, DateTime.UtcNow.AddDays(-1)),
            Product("Hidden", 50, DateTime.UtcNow, isActive: false));

        var result = await GetProductsAsync("sort=newest");

        Assert.Equal(["New", "Old"], result.Items.Select(p => p.Name));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Products_DefaultSort_IsNewest()
    {
        await SeedAsync(Product("Old", 1, DateTime.UtcNow.AddDays(-2)), Product("New", 1, DateTime.UtcNow));

        var result = await GetProductsAsync("");

        Assert.Equal(["New", "Old"], result.Items.Select(p => p.Name));
    }

    [Theory]
    [InlineData("price_asc", new[] { "Cheap", "Mid", "Pricey" })]
    [InlineData("price_desc", new[] { "Pricey", "Mid", "Cheap" })]
    public async Task Products_SortByPrice_OrdersByPrice(string sort, string[] expected)
    {
        await SeedAsync(Product("Mid", 200), Product("Pricey", 900), Product("Cheap", 50));

        var result = await GetProductsAsync($"sort={sort}");

        Assert.Equal(expected, result.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Products_FilterByCategory_ReturnsOnlyThatCategory()
    {
        await SeedAsync(Product("Shirt", 100, categoryId: 1), Product("Dress", 100, categoryId: 2));

        var result = await GetProductsAsync("category=women");

        Assert.Equal(["Dress"], result.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Products_Paging_SplitsResultsAndReportsTotals()
    {
        await SeedAsync(Enumerable.Range(1, 5).Select(i => Product($"P{i}", i * 10)).ToArray());

        var second = await GetProductsAsync("sort=price_asc&page=2&pageSize=2");

        Assert.Equal(["P3", "P4"], second.Items.Select(p => p.Name));
        Assert.Equal(5, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
    }

    [Fact]
    public async Task Products_CoverImage_IsLowestSortOrder()
    {
        var product = Product("Shirt", 100);
        product.Images.Add(new ProductImage { Url = "second.jpg", PublicId = "b", SortOrder = 1 });
        product.Images.Add(new ProductImage { Url = "cover.jpg", PublicId = "a", SortOrder = 0 });
        await SeedAsync(product);

        var result = await GetProductsAsync("");

        Assert.Equal("cover.jpg", result.Items.Single().CoverImageUrl);
    }

    [Theory]
    [InlineData("sort=oldest")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=51")]
    public async Task Products_InvalidQuery_ReturnsBadRequest(string query)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/products?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductBySlug_ReturnsDetailForActiveAndNotFoundForInactive()
    {
        var active = Product("Blue Shirt", 100);
        active.Sizes.Add("M");
        await SeedAsync(active, Product("Secret", 100, isActive: false));
        var client = _factory.CreateClient();

        var detail = await client.GetFromJsonAsync<ProductDetailDto>("/api/products/blue-shirt");
        var hidden = await client.GetAsync("/api/products/secret");

        Assert.Equal(["M"], detail!.Sizes);
        Assert.Equal("Men", detail.CategoryName);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_WithoutAdmin_ReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("Shoes", null));
        var list = await client.GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
    }

    [Fact]
    public async Task AdminCreateProduct_GeneratesUniqueSlugs()
    {
        var client = _factory.CreateAdminClient();
        // A new product is always a draft: it has no image yet, and nothing goes live without one.
        var request = new ProductRequest("Red Tee!", 1, "Soft", 450m, [" M ", "L", "m"], false);

        var first = await (await client.PostAsJsonAsync("/api/admin/products", request))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        var second = await (await client.PostAsJsonAsync("/api/admin/products", request))
            .Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.Equal("red-tee", first!.Slug);
        Assert.Equal("red-tee-2", second!.Slug);
        Assert.Equal(["M", "L"], first.Sizes);
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("Tee", 1, 0)]
    [InlineData("Tee", 1, 10.999)]
    [InlineData("Tee", 99, 10)]
    public async Task AdminCreateProduct_InvalidInput_ReturnsBadRequest(string name, int categoryId, double price)
    {
        var response = await _factory.CreateAdminClient().PostAsJsonAsync(
            "/api/admin/products", new ProductRequest(name, categoryId, null, (decimal)price, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateProduct_ChangesFieldsAndKeepsSlug()
    {
        var client = _factory.CreateAdminClient();
        var created = await (await client.PostAsJsonAsync(
                "/api/admin/products", new ProductRequest("Tee", 1, null, 100m, ["S"], false)))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        await AddImageAsync(created!.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/products/{created.Id}", new ProductRequest("Tee v2", 2, "New", 120m, ["S", "M"], true));
        var updated = await response.Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("tee", updated!.Slug);
        Assert.Equal("Tee v2", updated.Name);
        Assert.Equal(2, updated.CategoryId);
        Assert.Equal(["S", "M"], updated.Sizes);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task AdminDeleteProduct_DeactivatesAndHidesFromPublic()
    {
        var client = _factory.CreateAdminClient();
        var created = await (await client.PostAsJsonAsync(
                "/api/admin/products", new ProductRequest("Tee", 1, null, 100m, null, false)))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        // Published first, so the delete has something to withdraw from the storefront.
        await AddImageAsync(created!.Id);
        await client.PutAsJsonAsync(
            $"/api/admin/products/{created.Id}", new ProductRequest("Tee", 1, null, 100m, null, true));

        var delete = await client.DeleteAsync($"/api/admin/products/{created.Id}");
        var publicGet = await client.GetAsync("/api/products/tee");
        var adminGet = await client.GetFromJsonAsync<ProductDetailDto>($"/api/admin/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, publicGet.StatusCode);
        Assert.False(adminGet!.IsActive);
    }

    [Fact]
    public async Task AdminListProducts_IncludesInactiveAndSearchesByName()
    {
        await SeedAsync(Product("Blue Shirt", 100), Product("Red Shirt", 100, isActive: false), Product("Cap", 50));

        var result = await _factory.CreateAdminClient()
            .GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/admin/products?q=shirt&sort=price_asc");

        Assert.Equal(["Blue Shirt", "Red Shirt"], result!.Items.Select(p => p.Name).Order());
        Assert.False(result.Items.Single(p => p.Name == "Red Shirt").IsActive);
        Assert.True(result.Items.Single(p => p.Name == "Blue Shirt").IsActive);
    }

    [Fact]
    public async Task AdminCategories_CreateRenameDeleteAndRejectDuplicates()
    {
        var client = _factory.CreateAdminClient();

        var created = await (await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("Shoes & Boots", 5)))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var duplicate = await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("shoes & boots", null));
        var renamed = await (await client.PutAsJsonAsync($"/api/admin/categories/{created!.Id}", new CategoryRequest("Footwear", 6)))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var delete = await client.DeleteAsync($"/api/admin/categories/{created.Id}");

        Assert.Equal("shoes-boots", created.Slug);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(("shoes-boots", "Footwear"), (renamed!.Slug, renamed.Name));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteCategory_WithProducts_ReturnsConflict()
    {
        await SeedAsync(Product("Shirt", 100, categoryId: 1));

        var response = await _factory.CreateAdminClient().DeleteAsync("/api/admin/categories/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static Product Product(
        string name, decimal price, DateTime? createdAt = null, bool isActive = true, int categoryId = 1) => new()
    {
        Name = name,
        Slug = Slugs.From(name),
        Price = price,
        CategoryId = categoryId,
        IsActive = isActive,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    /// <summary>Gives a draft the one image it needs before it can be activated.</summary>
    private async Task AddImageAsync(int productId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.ProductImages.Add(new ProductImage
        {
            ProductId = productId,
            Url = "https://images.example.com/cover.jpg",
            PublicId = $"products/{productId}-cover",
            SortOrder = 0
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedAsync(params Product[] products)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private async Task<PagedResult<ProductSummaryDto>> GetProductsAsync(string query)
    {
        var result = await _factory.CreateClient()
            .GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?{query}");
        return result!;
    }
}
