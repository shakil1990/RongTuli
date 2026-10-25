using System.Diagnostics.CodeAnalysis;
using Shop.Api.Domain;

namespace Shop.Api.Orders;

/// <summary>
/// The moves an order can make. It goes forward one step at a time and can be cancelled until it
/// is delivered — a shipped order the customer refused comes back as cancelled. Delivered and
/// cancelled are final, so a mis-click cannot reopen an order the courier has settled.
/// </summary>
public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Next = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Cancelled],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> NextFrom(OrderStatus status) => Next[status];

    public static bool CanMove(OrderStatus from, OrderStatus to) => Next[from].Contains(to);

    /// <summary>Reads a status by name; see <see cref="EnumNames.TryParse{TEnum}"/>.</summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out OrderStatus? status) =>
        EnumNames.TryParse(value, out status);

    public static string Names => EnumNames.List<OrderStatus>();
}
