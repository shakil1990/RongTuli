using Microsoft.EntityFrameworkCore;
using Shop.Api.Cart;
using Shop.Api.Data;

namespace Shop.Tests;

/// <summary>
/// The endpoint tests run on the in-memory provider, which happily evaluates projections that
/// Npgsql cannot translate. These tests compile the query against the PostgreSQL provider —
/// <c>ToQueryString</c> translates without opening a connection — so a projection that would
/// only fail in production fails here instead.
/// </summary>
public sealed class CartQueryTranslationTests
{
    private static ShopDbContext CreateNpgsqlContext()
    {
        // Never connected to; the provider only needs a well-formed string to translate.
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_check")
            .Options;

        return new ShopDbContext(options);
    }

    [Fact]
    public void ProductsForQuote_TranslatesToPostgresSql()
    {
        using var db = CreateNpgsqlContext();

        var sql = CartQueries.ProductsForQuote(db, [1, 2, 3]).ToQueryString();

        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductsForQuote_SelectsTheSizesArrayAndTheCoverImage()
    {
        using var db = CreateNpgsqlContext();

        var sql = CartQueries.ProductsForQuote(db, [1]).ToQueryString();

        Assert.Contains("Sizes", sql);
        Assert.Contains("ProductImage", sql);
    }
}
