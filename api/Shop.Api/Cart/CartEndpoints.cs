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

            return Results.Ok(await CartPricing.QuoteAsync(db, request.Items!, ct));
        });

        return app;
    }
}
