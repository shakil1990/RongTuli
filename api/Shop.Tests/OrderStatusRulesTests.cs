using Shop.Api.Domain;
using Shop.Api.Orders;

namespace Shop.Tests;

public sealed class OrderStatusRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    public void CanMove_OneStepForwardOrToCancelled(OrderStatus from, OrderStatus to)
    {
        Assert.True(OrderStatusRules.CanMove(from, to));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Confirmed)]
    public void CanMove_RefusesSkippingBackwardsOrStandingStill(OrderStatus from, OrderStatus to)
    {
        Assert.False(OrderStatusRules.CanMove(from, to));
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void NextFrom_AFinalStatus_IsEmpty(OrderStatus status)
    {
        Assert.Empty(OrderStatusRules.NextFrom(status));
    }

    [Fact]
    public void NextFrom_CoversEveryStatus()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            Assert.NotNull(OrderStatusRules.NextFrom(status));
        }
    }

    [Theory]
    [InlineData("Confirmed", OrderStatus.Confirmed)]
    [InlineData("shipped", OrderStatus.Shipped)]
    [InlineData("  DELIVERED ", OrderStatus.Delivered)]
    public void TryParse_ReadsANameInAnyCase(string value, OrderStatus expected)
    {
        Assert.True(OrderStatusRules.TryParse(value, out var status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("Returned")]
    [InlineData("Pending,Shipped")]
    public void TryParse_RefusesAnythingButAName(string? value)
    {
        Assert.False(OrderStatusRules.TryParse(value, out _));
    }
}
