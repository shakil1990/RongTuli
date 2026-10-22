using Shop.Api.Domain;

namespace Shop.Api.Orders;

/// <summary>
/// Order settings, bound from the "Orders" section (<c>Orders__InsideDhakaFee</c>,
/// <c>Orders__OutsideDhakaFee</c>). None of it is secret.
/// </summary>
public sealed class OrderOptions
{
    public const string SectionName = "Orders";

    /// <summary>
    /// Delivery fees in taka, one per zone. Each order keeps the fee it was placed with, so
    /// changing one only affects orders placed afterwards.
    /// </summary>
    public decimal InsideDhakaFee { get; set; } = 80m;

    public decimal OutsideDhakaFee { get; set; } = 150m;

    public decimal FeeFor(DeliveryZone zone) => zone switch
    {
        DeliveryZone.InsideDhaka => InsideDhakaFee,
        DeliveryZone.OutsideDhaka => OutsideDhakaFee,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "Unknown delivery zone.")
    };
}
