using Microsoft.Extensions.Options;
using Shop.Api.Cart;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Phone;

namespace Shop.Api.Orders;

public enum PlaceOrderStatus
{
    Placed,

    /// <summary>The form's phone is not the one the token proves.</summary>
    PhoneMismatch,

    /// <summary>A line can no longer be ordered, or its price moved since the customer saw it.</summary>
    CartChanged
}

/// <summary><see cref="Order"/> is set when the order was placed, <see cref="Quote"/> when the cart changed under it.</summary>
public sealed record PlaceOrderResult(PlaceOrderStatus Status, Order? Order = null, CartQuoteDto? Quote = null);

/// <summary>
/// Turns a validated checkout form into an order. Every price comes from the database; the
/// browser's prices only decide whether the customer saw the same ones.
/// </summary>
internal sealed partial class OrderPlacement(
    ShopDbContext db,
    IOptions<OrderOptions> options,
    ILogger<OrderPlacement> logger)
{
    /// <param name="verifiedPhone">
    /// The phone the token proves, or null when phone verification is switched off and the form's
    /// number is taken as typed.
    /// </param>
    public async Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, string? verifiedPhone, CancellationToken ct)
    {
        // Validation has already checked the number, so it normalizes.
        PhoneNumbers.TryNormalize(request.Phone, out var phone);
        if (verifiedPhone is not null && !string.Equals(phone, verifiedPhone, StringComparison.Ordinal))
        {
            LogPhoneMismatch();
            return new PlaceOrderResult(PlaceOrderStatus.PhoneMismatch);
        }

        var quote = await CartPricing.QuoteAsync(db, request.Items!, ct);
        if (quote.Items.Any(line => !line.IsOrderable || line.Issues.Contains(CartQuoteIssues.PriceChanged)))
        {
            LogCartChanged();
            return new PlaceOrderResult(PlaceOrderStatus.CartChanged, Quote: quote);
        }

        // Validation has already checked the zone; the admin reviews every order regardless.
        EnumNames.TryParse<DeliveryZone>(request.DeliveryZone, out var zone);
        var deliveryFee = options.Value.FeeFor(zone!.Value);
        var order = new Order
        {
            CustomerName = request.CustomerName!.Trim(),
            Phone = phone!,
            Address = request.Address!.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = OrderStatus.Pending,
            DeliveryZone = zone.Value,
            DeliveryFee = deliveryFee,
            Total = quote.Subtotal + deliveryFee,
            CreatedAt = DateTime.UtcNow
        };

        // Name and price are copied, so editing the product later leaves this order as it was.
        foreach (var line in quote.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = line.ProductId,
                ProductName = line.Name!,
                Size = line.Size,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity
            });
        }

        // One SaveChanges is one transaction: the order and all its lines, or nothing.
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        LogPlaced(order.Id, order.Items.Count);
        return new PlaceOrderResult(PlaceOrderStatus.Placed, order);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Placed order {OrderId} with {LineCount} lines.")]
    private partial void LogPlaced(int orderId, int lineCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refused an order: the form's phone is not the verified one.")]
    private partial void LogPhoneMismatch();

    [LoggerMessage(Level = LogLevel.Information, Message = "Refused an order: the cart changed since it was last priced.")]
    private partial void LogCartChanged();
}
