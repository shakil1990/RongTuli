using Shop.Api.Cart;
using Shop.Api.Domain;
using Shop.Api.Phone;

namespace Shop.Api.Orders;

internal static class OrderValidation
{
    public const int MaxNameLength = 200;
    public const int MinAddressLength = 10;
    public const int MaxAddressLength = 500;
    public const int MaxNoteLength = 1000;

    /// <summary>The form's checks, plus the cart's own, with each message keyed to the field it belongs beside.</summary>
    public static Dictionary<string, string[]> Validate(PlaceOrderRequest request)
    {
        var errors = CartValidation.Validate(new CartQuoteRequest(request.Items));

        var name = request.CustomerName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
        {
            errors["customerName"] = [$"Enter the name to deliver to, at most {MaxNameLength} characters."];
        }

        if (!PhoneNumbers.TryNormalize(request.Phone, out _))
        {
            errors["phone"] = [PhoneNumbers.FormatHint];
        }

        var address = request.Address?.Trim();
        if (string.IsNullOrEmpty(address) || address.Length < MinAddressLength || address.Length > MaxAddressLength)
        {
            errors["address"] =
            [
                $"Enter the full delivery address — house, road, area and city — in {MinAddressLength} to {MaxAddressLength} characters."
            ];
        }

        if (!EnumNames.TryParse<DeliveryZone>(request.DeliveryZone, out _))
        {
            errors["deliveryZone"] = ["Choose where the order is delivered: inside or outside Dhaka City."];
        }

        if ((request.Note?.Trim().Length ?? 0) > MaxNoteLength)
        {
            errors["note"] = [$"Keep the note to {MaxNoteLength} characters."];
        }

        // Without the price the customer saw there is nothing to hold the charge against.
        if (request.Items is { Count: > 0 } items && items.Any(item => item.UnitPrice is null))
        {
            errors["items[].unitPrice"] = ["Every line needs the price that was shown."];
        }

        return errors;
    }
}
