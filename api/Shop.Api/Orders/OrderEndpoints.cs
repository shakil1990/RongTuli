using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Orders;

public static class OrderEndpoints
{
    private const string PhoneMismatchMessage =
        "This is not the number you verified. Verify this number, or go back to the verified one.";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var requirePhoneVerification =
            app.ServiceProvider.GetRequiredService<IOptions<OrderOptions>>().Value.RequirePhoneVerification;

        // Public, so the checkout page can show the full total before the phone is verified, and
        // knows whether to ask for a code at all.
        app.MapGet("/api/checkout/options", (IOptions<OrderOptions> options) =>
            Results.Ok(new CheckoutOptionsDto(
                Enum.GetValues<DeliveryZone>()
                    .Select(zone => new DeliveryOptionDto(zone.ToString(), options.Value.FeeFor(zone)))
                    .ToList(),
                options.Value.RequirePhoneVerification)));

        var placeOrder = app.MapPost("/api/orders", async (
            PlaceOrderRequest request,
            OrderPlacement placement,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var errors = OrderValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var verifiedPhone = requirePhoneVerification ? VerifiedPhone(user) : null;
            var result = await placement.PlaceAsync(request, verifiedPhone, ct);
            return result.Status switch
            {
                PlaceOrderStatus.Placed => Results.Created(
                    $"/api/orders/{result.Order!.Id}", OrderQueries.ToDto(result.Order)),
                PlaceOrderStatus.PhoneMismatch => Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["phone"] = [PhoneMismatchMessage] }),
                // The fresh quote rides along so the page can show what changed without asking again.
                _ => Results.Problem(
                    "Something in your cart changed since you last saw it. Check the new total and place the order again.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    extensions: new Dictionary<string, object?> { ["quote"] = result.Quote })
            };
        }).RequireRateLimiting(RateLimits.PlaceOrder);

        if (requirePhoneVerification)
        {
            placeOrder.RequireAuthorization(AdminAuthorization.VerifiedPhonePolicy);
        }

        // Reading orders always needs a verified phone, and only ever shows that phone's orders.
        var group = app.MapGroup("/api/orders").RequireAuthorization(AdminAuthorization.VerifiedPhonePolicy);

        group.MapGet("/", async (ShopDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var phone = VerifiedPhone(user);
            var orders = await OrderQueries.Summaries(db.Orders.Where(o => o.Phone == phone))
                .Take(OrderQueries.MaxCustomerOrders)
                .ToListAsync(ct);
            return Results.Ok(orders);
        });

        // Another phone's order answers 404, not 403, so an id says nothing about whether it exists.
        group.MapGet("/{id:int}", async (int id, ShopDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var order = await OrderQueries.LoadAsync(db, id, ct);
            return order is null || order.Phone != VerifiedPhone(user)
                ? Results.NotFound()
                : Results.Ok(OrderQueries.ToDto(order));
        });

        return app;
    }

    // Only called where the verified-phone policy has already required the claim, so it is always there.
    private static string VerifiedPhone(ClaimsPrincipal user) => user.FindFirstValue(ClaimNames.Phone)!;
}
