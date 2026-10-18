namespace Shop.Api.Cart;

/// <summary>
/// One line of the browser's cart. <paramref name="UnitPrice"/> is the price the browser last saw,
/// sent back so the quote can flag a price change; it is never trusted as the price to charge.
/// </summary>
public sealed record CartQuoteItemRequest(int? ProductId, string? Size, int? Quantity, decimal? UnitPrice);

public sealed record CartQuoteRequest(IReadOnlyList<CartQuoteItemRequest>? Items);

/// <summary>
/// A priced cart line. <paramref name="UnitPrice"/> and <paramref name="LineTotal"/> come from the
/// database, so the browser can correct itself from this response.
/// </summary>
public sealed record CartQuoteLineDto(
    int ProductId,
    string? Name,
    string? Slug,
    string? CoverImageUrl,
    string? Size,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsOrderable,
    IReadOnlyList<string> Issues);

public sealed record CartQuoteDto(
    IReadOnlyList<CartQuoteLineDto> Items,
    decimal Subtotal,
    int ItemCount,
    bool HasIssues);

/// <summary>Issue codes a cart line can carry. The browser maps these to its own wording.</summary>
public static class CartQuoteIssues
{
    /// <summary>The product was deleted or deactivated since it went into the cart.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>The current price differs from the one the browser last saw.</summary>
    public const string PriceChanged = "price_changed";

    /// <summary>The chosen size is no longer offered for this product.</summary>
    public const string SizeUnavailable = "size_unavailable";
}
