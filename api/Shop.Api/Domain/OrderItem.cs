namespace Shop.Api.Domain;

/// <summary>
/// One line of an order. Name and price are copied at order time so later product edits don't change old orders.
/// </summary>
public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    /// <summary>Null for a one-size product.</summary>
    public string? Size { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
