using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Orders;

namespace Shop.Tests;

/// <summary>
/// As in <see cref="CartQueryTranslationTests"/>: the order lists are compiled against the
/// PostgreSQL provider, since the in-memory one would accept a projection Npgsql cannot translate.
/// </summary>
public sealed class OrderQueryTranslationTests
{
    [Fact]
    public void Summaries_FilteredByStatusAndPhone_TranslateToPostgresSql()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_check")
            .Options;
        using var db = new ShopDbContext(options);

        var orders = db.Orders.Where(o => o.Status == OrderStatus.Pending && o.Phone == "+8801712345678");
        var sql = OrderQueries.Summaries(orders).Skip(25).Take(25).ToQueryString();

        Assert.Contains("OrderItem", sql);
        Assert.Contains("ORDER BY", sql);
    }
}
