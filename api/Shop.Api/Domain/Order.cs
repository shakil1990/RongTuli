namespace Shop.Api.Domain;

public sealed class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Note { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<OrderItem> Items { get; } = new List<OrderItem>();
}
