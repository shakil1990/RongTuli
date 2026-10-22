using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Api.Orders;

public static class OrderQueries
{
    /// <summary>A customer's list is short; past this many the oldest are left off.</summary>
    public const int MaxCustomerOrders = 50;

    public static Task<Order?> LoadAsync(ShopDbContext db, int id, CancellationToken ct) =>
        db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public static IQueryable<OrderSummaryDto> Summaries(IQueryable<Order> orders) =>
        orders
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.Status.ToString(),
                o.CustomerName,
                o.Phone,
                o.DeliveryZone.ToString(),
                o.Items.Sum(i => i.Quantity),
                o.Total,
                o.CreatedAt));

    public static OrderDto ToDto(Order order)
    {
        var items = order.Items
            .OrderBy(i => i.Id)
            .Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.Size, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList();

        return new OrderDto(
            order.Id,
            order.Status.ToString(),
            order.CustomerName,
            order.Phone,
            order.Address,
            order.Note,
            order.DeliveryZone.ToString(),
            items.Sum(i => i.LineTotal),
            order.DeliveryFee,
            order.Total,
            order.CreatedAt,
            items);
    }

    public static AdminOrderDto ToAdminDto(Order order) => new(
        ToDto(order),
        OrderStatusRules.NextFrom(order.Status).Select(status => status.ToString()).ToList());
}
