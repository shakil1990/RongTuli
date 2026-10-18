namespace Shop.Api.Cart;

internal static class CartValidation
{
    public const int MaxLines = 50;
    public const int MaxQuantity = 99;

    public static Dictionary<string, string[]> Validate(CartQuoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var items = request.Items;

        if (items is null || items.Count == 0)
        {
            errors["items"] = ["At least one cart line is required."];
            return errors;
        }

        if (items.Count > MaxLines)
        {
            errors["items"] = [$"A cart may hold at most {MaxLines} lines."];
            return errors;
        }

        if (items.Any(item => item.ProductId is null or < 1))
        {
            errors["items[].productId"] = ["Every cart line needs a product id."];
        }

        if (items.Any(item => item.Quantity is null or < 1 or > MaxQuantity))
        {
            errors["items[].quantity"] = [$"Quantity must be between 1 and {MaxQuantity}."];
        }

        return errors;
    }
}
