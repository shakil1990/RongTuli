using Shop.Api.Cart;

namespace Shop.Api.Orders;

/// <summary>
/// What the checkout form sends. Each line's <c>UnitPrice</c> is the price the customer was shown;
/// it is never charged, only compared, so an order is refused rather than placed at a price they
/// did not see. <see cref="DeliveryZone"/> is the name of a <c>DeliveryZone</c> and is required.
/// </summary>
public sealed record PlaceOrderRequest(
    string? CustomerName,
    string? Phone,
    string? Address,
    string? Note,
    string? DeliveryZone,
    IReadOnlyList<CartQuoteItemRequest>? Items);

public sealed record OrderItemDto(
    int ProductId,
    string ProductName,
    string? Size,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

/// <summary>An order with its lines. <see cref="Status"/> and <see cref="DeliveryZone"/> are enum names.</summary>
public sealed record OrderDto(
    int Id,
    string Status,
    string CustomerName,
    string Phone,
    string Address,
    string? Note,
    string DeliveryZone,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

/// <summary>One row of an order list: enough to find the order, not its lines.</summary>
public sealed record OrderSummaryDto(
    int Id,
    string Status,
    string CustomerName,
    string Phone,
    string DeliveryZone,
    int ItemCount,
    decimal Total,
    DateTime CreatedAt);

/// <summary>An order as the admin sees it, with the statuses it may move to next.</summary>
public sealed record AdminOrderDto(OrderDto Order, IReadOnlyList<string> NextStatuses);

public sealed record OrderStatusRequest(string? Status);

public sealed record DeliveryOptionDto(string Zone, decimal Fee);

/// <summary>
/// What the checkout page needs to offer the delivery zones, show a total before ordering, and
/// decide whether to ask for a texted code or just the number.
/// </summary>
public sealed record CheckoutOptionsDto(IReadOnlyList<DeliveryOptionDto> DeliveryOptions, bool RequirePhoneVerification);
