using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;

namespace Shop.Api.Cart;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // Re-prices the browser's cart from the database so a changed price or a withdrawn
        // product is flagged before checkout. Public: an anonymous viewer may fill a cart.
        app.MapPost("/api/cart/quote", async (CartQuoteRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            var errors = CartValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var lines = request.Items!;
            var productIds = lines.Select(line => line.ProductId!.Value).Distinct().ToArray();
            var products = await CartQueries.ProductsForQuote(db, productIds)
                .ToDictionaryAsync(product => product.Id, ct);

            var quoted = lines
                .Select(line => Quote(line, products.GetValueOrDefault(line.ProductId!.Value)))
                .ToList();

            return Results.Ok(new CartQuoteDto(
                quoted,
                quoted.Where(line => line.IsOrderable).Sum(line => line.LineTotal),
                quoted.Where(line => line.IsOrderable).Sum(line => line.Quantity),
                quoted.Any(line => line.Issues.Count > 0)));
        });

        return app;
    }

    private static CartQuoteLineDto Quote(CartQuoteItemRequest line, CartQuoteProduct? product)
    {
        var quantity = line.Quantity!.Value;
        var size = string.IsNullOrWhiteSpace(line.Size) ? null : line.Size.Trim();

        if (product is null || !product.IsActive)
        {
            return new CartQuoteLineDto(
                line.ProductId!.Value, product?.Name, product?.Slug, product?.CoverImageUrl,
                size, quantity, 0m, 0m, false, [CartQuoteIssues.Unavailable]);
        }

        var issues = new List<string>();

        // A product with no size list is one-size-fits-all, so any size the browser kept is dropped.
        var hasSizes = product.Sizes.Count > 0;
        var matchedSize = hasSizes
            ? product.Sizes.FirstOrDefault(offered => string.Equals(offered, size, StringComparison.OrdinalIgnoreCase))
            : null;
        if (hasSizes && matchedSize is null)
        {
            issues.Add(CartQuoteIssues.SizeUnavailable);
        }

        if (line.UnitPrice is not null && line.UnitPrice != product.Price)
        {
            issues.Add(CartQuoteIssues.PriceChanged);
        }

        // A changed price only needs showing; a missing size has to be picked again before ordering.
        var isOrderable = !hasSizes || matchedSize is not null;

        return new CartQuoteLineDto(
            product.Id,
            product.Name,
            product.Slug,
            product.CoverImageUrl,
            hasSizes ? matchedSize ?? size : null,
            quantity,
            product.Price,
            isOrderable ? product.Price * quantity : 0m,
            isOrderable,
            issues);
    }
}
