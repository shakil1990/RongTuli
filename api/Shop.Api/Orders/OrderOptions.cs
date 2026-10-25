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

    /// <summary>
    /// Whether placing an order needs a verified-phone token (<c>Orders__RequirePhoneVerification</c>).
    /// Off, anyone can order with a well-formed number and the admin's confirmation call is the only
    /// check — meant for while no SMS provider is set up. Read once at startup, when the endpoints are mapped.
    /// </summary>
    public bool RequirePhoneVerification { get; set; } = true;

    public decimal FeeFor(DeliveryZone zone) => zone switch
    {
        DeliveryZone.InsideDhaka => InsideDhakaFee,
        DeliveryZone.OutsideDhaka => OutsideDhakaFee,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, "Unknown delivery zone.")
    };
}
