using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Orders;

namespace Shop.Tests;

public sealed class AdminOrderApiTests : IDisposable
{
    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("GET", "/api/admin/orders")]
    [InlineData("GET", "/api/admin/orders/counts")]
    [InlineData("GET", "/api/admin/orders/1")]
    [InlineData("PUT", "/api/admin/orders/1/status")]
    public async Task Endpoints_WithoutAdmin_ReturnUnauthorized(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new OrderStatusRequest("Confirmed"))
        };

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_NewestFirst_FilteredByStatus_AndPaged()
    {
        var pending = await SeedOrdersAsync(
            (OrderStatus.Pending, 3), (OrderStatus.Confirmed, 2), (OrderStatus.Pending, 1));

        var page = await AdminGetAsync<PagedResult<OrderSummaryDto>>("/api/admin/orders?status=pending&pageSize=1");

        Assert.Equal((2, 2), (page.TotalCount, page.TotalPages));
        Assert.Equal(pending[2].Id, page.Items.Single().Id);
        Assert.Equal("Pending", page.Items.Single().Status);
    }

    [Fact]
    public async Task List_WithoutAFilter_ReturnsEveryOrder()
    {
        await SeedOrdersAsync((OrderStatus.Pending, 1), (OrderStatus.Cancelled, 1));

        var page = await AdminGetAsync<PagedResult<OrderSummaryDto>>("/api/admin/orders");

        Assert.Equal(2, page.TotalCount);
    }

    [Theory]
    [InlineData("Lost")]
    [InlineData("2")]
    public async Task List_UnknownStatus_ReturnsBadRequest(string status)
    {
        var response = await _factory.CreateAdminClient().GetAsync($"/api/admin/orders?status={status}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Counts_ListEveryStatusIncludingZeros()
    {
        await SeedOrdersAsync((OrderStatus.Pending, 1), (OrderStatus.Pending, 1), (OrderStatus.Shipped, 1));

        var counts = await AdminGetAsync<Dictionary<string, int>>("/api/admin/orders/counts");

        Assert.Equal(
            new Dictionary<string, int>
            {
                ["Pending"] = 2, ["Confirmed"] = 0, ["Shipped"] = 1, ["Delivered"] = 0, ["Cancelled"] = 0
            },
            counts);
    }

    [Fact]
    public async Task Get_ReturnsTheOrderWithItsNextStatuses()
    {
        var order = (await SeedOrdersAsync((OrderStatus.Confirmed, 2))).Single();

        var detail = await AdminGetAsync<AdminOrderDto>($"/api/admin/orders/{order.Id}");

        Assert.Equal(["Shipped", "Cancelled"], detail.NextStatuses);
        Assert.Equal((2, 900m, 80m, 980m), (detail.Order.Items.Single().Quantity, detail.Order.Subtotal, detail.Order.DeliveryFee, detail.Order.Total));
    }

    [Fact]
    public async Task Get_UnknownOrder_ReturnsNotFound()
    {
        var response = await _factory.CreateAdminClient().GetAsync("/api/admin/orders/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetStatus_AllowedMove_SavesItAndReturnsTheNextOptions()
    {
        var order = (await SeedOrdersAsync((OrderStatus.Pending, 1))).Single();

        var response = await SetStatusAsync(order.Id, "confirmed");

        response.EnsureSuccessStatusCode();
        var detail = (await response.Content.ReadFromJsonAsync<AdminOrderDto>())!;
        Assert.Equal("Confirmed", detail.Order.Status);
        Assert.Equal(["Shipped", "Cancelled"], detail.NextStatuses);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, "Delivered")]
    [InlineData(OrderStatus.Delivered, "Pending")]
    [InlineData(OrderStatus.Cancelled, "Confirmed")]
    public async Task SetStatus_MoveNotAllowed_ReturnsConflictAndLeavesTheOrder(OrderStatus from, string to)
    {
        var order = (await SeedOrdersAsync((from, 1))).Single();

        var response = await SetStatusAsync(order.Id, to);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var detail = await AdminGetAsync<AdminOrderDto>($"/api/admin/orders/{order.Id}");
        Assert.Equal(from.ToString(), detail.Order.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("Returned")]
    public async Task SetStatus_NotAStatus_ReturnsBadRequestOnTheStatusField(string? status)
    {
        var order = (await SeedOrdersAsync((OrderStatus.Pending, 1))).Single();

        var response = await SetStatusAsync(order.Id, status);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("status", problem!.Errors.Keys);
    }

    [Fact]
    public async Task SetStatus_UnknownOrder_ReturnsNotFound()
    {
        var response = await SetStatusAsync(999, "Confirmed");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>One order per entry, a minute apart in the order given, each of one 450-taka product.</summary>
    private async Task<List<Order>> SeedOrdersAsync(params (OrderStatus Status, int Quantity)[] orders)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var product = new Product
        {
            Name = "Shirt", Slug = Slugs.From("Shirt") + Guid.NewGuid().ToString("N")[..6], Price = 450m,
            CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);

        var start = DateTime.UtcNow.AddHours(-1);
        var saved = orders.Select((spec, index) =>
        {
            var order = new Order
            {
                CustomerName = "Rahim Uddin",
                Phone = "+8801712345678",
                Address = "House 12, Road 5, Dhanmondi, Dhaka",
                Status = spec.Status,
                DeliveryZone = DeliveryZone.InsideDhaka,
                DeliveryFee = 80m,
                Total = 450m * spec.Quantity + 80m,
                CreatedAt = start.AddMinutes(index)
            };
            order.Items.Add(new OrderItem
            {
                Product = product, ProductName = "Shirt", Size = "M", UnitPrice = 450m, Quantity = spec.Quantity
            });
            return order;
        }).ToList();

        db.Orders.AddRange(saved);
        await db.SaveChangesAsync();
        return saved;
    }

    private async Task<T> AdminGetAsync<T>(string path) =>
        (await _factory.CreateAdminClient().GetFromJsonAsync<T>(path))!;

    private Task<HttpResponseMessage> SetStatusAsync(int id, string? status) =>
        _factory.CreateAdminClient().PutAsJsonAsync($"/api/admin/orders/{id}/status", new OrderStatusRequest(status));
}
