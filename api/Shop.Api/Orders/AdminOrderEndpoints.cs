using Microsoft.EntityFrameworkCore;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Orders;

public static class AdminOrderEndpoints
{
    public static IEndpointRouteBuilder MapAdminOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/orders").RequireAuthorization(AdminAuthorization.Policy);

        group.MapGet("/", async (ShopDbContext db, string? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            var pageNumber = page ?? 1;
            var size = pageSize ?? CatalogValidation.DefaultPageSize;
            var errors = CatalogValidation.ValidateListQuery(sort: null, pageNumber, size);

            OrderStatus? filter = null;
            if (status is not null)
            {
                if (OrderStatusRules.TryParse(status, out var parsed))
                {
                    filter = parsed;
                }
                else
                {
                    errors["status"] = [$"Status must be one of: {OrderStatusRules.Names}."];
                }
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var orders = filter is null ? db.Orders : db.Orders.Where(o => o.Status == filter);
            var total = await orders.CountAsync(ct);
            var items = await OrderQueries.Summaries(orders)
                .Skip((pageNumber - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<OrderSummaryDto>(
                items, pageNumber, size, total, (int)Math.Ceiling(total / (double)size)));
        });

        // Every status is listed, zeros included, so the panel can show a Pending count of 0.
        group.MapGet("/counts", async (ShopDbContext db, CancellationToken ct) =>
        {
            var counts = await db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

            return Results.Ok(Enum.GetValues<OrderStatus>()
                .ToDictionary(s => s.ToString(), s => counts.GetValueOrDefault(s)));
        });

        group.MapGet("/{id:int}", async (int id, ShopDbContext db, CancellationToken ct) =>
        {
            var order = await OrderQueries.LoadAsync(db, id, ct);
            return order is null ? Results.NotFound() : Results.Ok(OrderQueries.ToAdminDto(order));
        });

        group.MapPut("/{id:int}/status", async (int id, OrderStatusRequest request, ShopDbContext db, CancellationToken ct) =>
        {
            if (!OrderStatusRules.TryParse(request.Status, out var next))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = [$"Status must be one of: {OrderStatusRules.Names}."]
                });
            }

            var order = await db.Orders.FindAsync([id], ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            if (!OrderStatusRules.CanMove(order.Status, next.Value))
            {
                return Results.Problem(
                    $"A {Describe(order.Status)} order cannot be marked {Describe(next.Value)}.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict");
            }

            order.Status = next.Value;
            await db.SaveChangesAsync(ct);

            var updated = await OrderQueries.LoadAsync(db, id, ct);
            return Results.Ok(OrderQueries.ToAdminDto(updated!));
        });

        return app;
    }

    private static string Describe(OrderStatus status) => status.ToString().ToLowerInvariant();
}
