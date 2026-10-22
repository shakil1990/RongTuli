namespace Shop.Api.Domain;

/// <summary>
/// A cash-on-delivery order. Customers have no account, so <see cref="UserId"/> is null for every
/// order they place; the phone identifies them, and it was verified by OTP first.
/// </summary>
public sealed class Order
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Note { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<OrderItem> Items { get; } = new List<OrderItem>();
}
