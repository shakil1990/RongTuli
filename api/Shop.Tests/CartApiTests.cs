using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Cart;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Tests;

public sealed class CartApiTests : IDisposable
{
    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Quote_PricesLinesFromTheDatabaseAndTotalsThem()
    {
        var shirt = Product("Shirt", 450m);
        var cap = Product("Cap", 200m);
        await SeedAsync(shirt, cap);

        var quote = await QuoteAsync(
            new CartQuoteItemRequest(shirt.Id, "M", 2, 450m),
            new CartQuoteItemRequest(cap.Id, null, 1, 200m));

        Assert.Equal(1100m, quote.Subtotal);
        Assert.Equal(3, quote.ItemCount);
        Assert.False(quote.HasIssues);
        Assert.All(quote.Items, line => Assert.True(line.IsOrderable));
    }

    [Fact]
    public async Task Quote_ReturnsNameAndCoverImageSoTheBrowserCanRefreshItsCart()
    {
        var shirt = Product("Blue Shirt", 450m);
        shirt.Images.Add(new ProductImage { Url = "cover.jpg", PublicId = "a", SortOrder = 0 });
        shirt.Images.Add(new ProductImage { Url = "second.jpg", PublicId = "b", SortOrder = 1 });
        await SeedAsync(shirt);

        var line = (await QuoteAsync(new CartQuoteItemRequest(shirt.Id, "M", 1, 450m))).Items.Single();

        Assert.Equal(("Blue Shirt", "blue-shirt", "cover.jpg"), (line.Name, line.Slug, line.CoverImageUrl));
    }

    [Fact]
    public async Task Quote_DeactivatedProduct_IsFlaggedAndLeftOutOfTheSubtotal()
    {
        var withdrawn = Product("Withdrawn", 500m, isActive: false);
        var kept = Product("Kept", 100m);
        await SeedAsync(withdrawn, kept);

        var quote = await QuoteAsync(
            new CartQuoteItemRequest(withdrawn.Id, "M", 1, 500m),
            new CartQuoteItemRequest(kept.Id, "M", 1, 100m));

        var flagged = quote.Items.First(line => line.ProductId == withdrawn.Id);
        Assert.Equal([CartQuoteIssues.Unavailable], flagged.Issues);
        Assert.False(flagged.IsOrderable);
        Assert.Equal(0m, flagged.LineTotal);
        Assert.Equal(100m, quote.Subtotal);
        Assert.Equal(1, quote.ItemCount);
        Assert.True(quote.HasIssues);
    }

    [Fact]
    public async Task Quote_UnknownProduct_IsFlaggedUnavailable()
    {
        var quote = await QuoteAsync(new CartQuoteItemRequest(4321, "M", 1, 100m));

        var line = quote.Items.Single();
        Assert.Equal([CartQuoteIssues.Unavailable], line.Issues);
        Assert.Null(line.Name);
        Assert.Equal(0m, quote.Subtotal);
    }

    [Fact]
    public async Task Quote_ChangedPrice_IsFlaggedButStaysOrderableAtTheCurrentPrice()
    {
        var shirt = Product("Shirt", 500m);
        await SeedAsync(shirt);

        var line = (await QuoteAsync(new CartQuoteItemRequest(shirt.Id, "M", 2, 450m))).Items.Single();

        Assert.Equal([CartQuoteIssues.PriceChanged], line.Issues);
        Assert.True(line.IsOrderable);
        Assert.Equal(500m, line.UnitPrice);
        Assert.Equal(1000m, line.LineTotal);
    }

    [Fact]
    public async Task Quote_WithoutAKnownPrice_DoesNotReportAPriceChange()
    {
        var shirt = Product("Shirt", 500m);
        await SeedAsync(shirt);

        var line = (await QuoteAsync(new CartQuoteItemRequest(shirt.Id, "M", 1, null))).Items.Single();

        Assert.Empty(line.Issues);
        Assert.Equal(500m, line.UnitPrice);
    }

    [Fact]
    public async Task Quote_SizeNoLongerOffered_IsFlaggedAndNotOrderable()
    {
        var shirt = Product("Shirt", 500m);
        shirt.Sizes.Add("M");
        await SeedAsync(shirt);

        var line = (await QuoteAsync(new CartQuoteItemRequest(shirt.Id, "XXL", 1, 500m))).Items.Single();

        Assert.Equal([CartQuoteIssues.SizeUnavailable], line.Issues);
        Assert.False(line.IsOrderable);
        Assert.Equal(0m, line.LineTotal);
    }

    [Fact]
    public async Task Quote_MatchesSizeIgnoringCaseAndReturnsTheOfferedSpelling()
    {
        var shirt = Product("Shirt", 500m);
        shirt.Sizes.Add("M");
        await SeedAsync(shirt);

        var line = (await QuoteAsync(new CartQuoteItemRequest(shirt.Id, " m ", 1, 500m))).Items.Single();

        Assert.Empty(line.Issues);
        Assert.Equal("M", line.Size);
    }

    [Fact]
    public async Task Quote_ProductWithoutSizes_IgnoresTheSizeTheBrowserKept()
    {
        var cap = Product("Cap", 200m);
        await SeedAsync(cap);

        var line = (await QuoteAsync(new CartQuoteItemRequest(cap.Id, "M", 1, 200m))).Items.Single();

        Assert.Empty(line.Issues);
        Assert.True(line.IsOrderable);
        Assert.Null(line.Size);
    }

    [Fact]
    public async Task Quote_KeepsDuplicateLinesSeparateSoEachSizeIsPricedOnItsOwn()
    {
        var shirt = Product("Shirt", 100m);
        shirt.Sizes.Add("M");
        shirt.Sizes.Add("L");
        await SeedAsync(shirt);

        var quote = await QuoteAsync(
            new CartQuoteItemRequest(shirt.Id, "M", 1, 100m),
            new CartQuoteItemRequest(shirt.Id, "L", 2, 100m));

        Assert.Equal(["M", "L"], quote.Items.Select(line => line.Size));
        Assert.Equal(300m, quote.Subtotal);
    }

    [Fact]
    public async Task Quote_EmptyCart_ReturnsBadRequest()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/cart/quote", new CartQuoteRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 100)]
    public async Task Quote_InvalidLine_ReturnsBadRequest(int productId, int quantity)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/cart/quote", new CartQuoteRequest([new CartQuoteItemRequest(productId, "M", quantity, 100m)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Quote_TooManyLines_ReturnsBadRequest()
    {
        // One past the 50-line cap in CartValidation.
        var lines = Enumerable.Range(1, 51)
            .Select(i => new CartQuoteItemRequest(i, "M", 1, 100m))
            .ToList();

        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/cart/quote", new CartQuoteRequest(lines));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Product Product(string name, decimal price, bool isActive = true, int categoryId = 1) => new()
    {
        Name = name,
        Slug = Slugs.From(name),
        Price = price,
        CategoryId = categoryId,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private async Task SeedAsync(params Product[] products)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private async Task<CartQuoteDto> QuoteAsync(params CartQuoteItemRequest[] items)
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/cart/quote", new CartQuoteRequest(items));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CartQuoteDto>())!;
    }
}
