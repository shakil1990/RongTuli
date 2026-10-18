using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;

namespace Shop.Api.Cart;

/// <summary>The product facts a cart line needs, loaded in one round trip.</summary>
public sealed record CartQuoteProduct(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    bool IsActive,
    List<string> Sizes,
    string? CoverImageUrl);

public static class CartQueries
{
    /// <summary>
    /// Loads every product a cart refers to, active or not, so a withdrawn one can be named
    /// in the quote rather than silently vanishing.
    ///
    /// Exposed (rather than inlined in the endpoint) so a test can assert the projection still
    /// translates to PostgreSQL: <c>Sizes</c> is a <c>text[]</c> column and the in-memory provider
    /// the endpoint tests run on accepts shapes Npgsql would reject.
    /// </summary>
    public static IQueryable<CartQuoteProduct> ProductsForQuote(ShopDbContext db, int[] productIds) =>
        db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new CartQuoteProduct(
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.IsActive,
                p.Sizes.ToList(),
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()));
}
