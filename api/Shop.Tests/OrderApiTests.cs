using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Cart;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Orders;
using Shop.Api.Phone;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// Placing and reading orders as a customer. Verified-phone tokens are issued straight from the
/// API's own <see cref="TokenIssuer"/>, the way phone verification issues them; one test goes
/// through the whole text-a-code flow to show the two halves meet.
/// </summary>
public sealed class OrderApiTests : IDisposable
{
    private const string Phone = "+8801712345678";
    private const string OtherPhone = "+8801812345678";
    private const decimal InsideDhakaFee = 80m;
    private const decimal OutsideDhakaFee = 150m;

    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Place_WithAVerifiedPhone_SavesAPendingOrderPricedFromTheDatabase()
    {
        var shirt = Product("Blue Shirt", 450m, "M", "L");
        var cap = Product("Cap", 200m);
        await SeedAsync(shirt, cap);

        var response = await PlaceAsync(
            Phone,
            Request("01712-345678", Line(shirt, "m", 2), Line(cap, null, 1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal($"/api/orders/{order.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(Phone, order.Phone);
        Assert.Equal(("InsideDhaka", 1100m, InsideDhakaFee, 1180m), (order.DeliveryZone, order.Subtotal, order.DeliveryFee, order.Total));
        Assert.Equal(
            [new OrderItemDto(shirt.Id, "Blue Shirt", "M", 450m, 2, 900m), new OrderItemDto(cap.Id, "Cap", null, 200m, 1, 200m)],
            order.Items);

        var saved = await LoadOrderAsync(order.Id);
        Assert.Null(saved.UserId);
        Assert.Equal(("Rahim Uddin", "House 12, Road 5, Dhanmondi, Dhaka"), (saved.CustomerName, saved.Address));
    }

    [Fact]
    public async Task Place_WithoutAToken_ReturnsUnauthorized()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/orders", Request(Phone, Line(shirt)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await OrdersAsync());
    }

    [Fact]
    public async Task Place_WithAnAdminToken_ReturnsForbidden()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);
        var admin = Issuer().Issue(new User
        {
            Id = 1, Email = TestSettings.AdminEmail, Name = "Admin", Role = UserRole.Admin, GoogleSub = "sub"
        });

        var response = await ClientWith(admin.Token).PostAsJsonAsync("/api/orders", Request(Phone, Line(shirt)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Place_PhoneOnTheFormIsNotTheVerifiedOne_ReturnsBadRequestOnThePhoneField()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);

        var response = await PlaceAsync(Phone, Request(OtherPhone, Line(shirt)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["phone"], problem!.Errors.Keys);
        Assert.Empty(await OrdersAsync());
    }

    [Fact]
    public async Task Place_PriceChangedSinceTheCustomerSawIt_ReturnsConflictWithTheNewQuote()
    {
        var shirt = Product("Shirt", 500m);
        await SeedAsync(shirt);

        var response = await PlaceAsync(Phone, Request(Phone, Line(shirt, price: 450m)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var quote = await QuoteFromConflictAsync(response);
        Assert.Equal(500m, quote.Subtotal);
        Assert.Equal([CartQuoteIssues.PriceChanged], quote.Items.Single().Issues);
        Assert.Empty(await OrdersAsync());
    }

    [Fact]
    public async Task Place_ProductDeactivated_ReturnsConflictAndSavesNothing()
    {
        var withdrawn = Product("Withdrawn", 300m, isActive: false);
        var kept = Product("Kept", 100m);
        await SeedAsync(withdrawn, kept);

        var response = await PlaceAsync(Phone, Request(Phone, Line(kept), Line(withdrawn)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var quote = await QuoteFromConflictAsync(response);
        Assert.False(quote.Items.Single(line => line.ProductId == withdrawn.Id).IsOrderable);
        Assert.Empty(await OrdersAsync());
    }

    [Fact]
    public async Task Place_SizeNoLongerOffered_ReturnsConflict()
    {
        var shirt = Product("Shirt", 450m, "M");
        await SeedAsync(shirt);

        var response = await PlaceAsync(Phone, Request(Phone, Line(shirt, "XL")));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Place_MissingDetails_ReturnsBadRequestForEachField()
    {
        var response = await PlaceAsync(
            Phone,
            new PlaceOrderRequest(" ", Phone, "Dhaka", new string('x', 1001), DeliveryZone: null, []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equivalent(new[] { "customerName", "address", "deliveryZone", "note", "items" }, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Place_LineWithoutTheShownPrice_ReturnsBadRequest()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);

        var response = await PlaceAsync(
            Phone,
            Request(Phone, new CartQuoteItemRequest(shirt.Id, null, 1, UnitPrice: null)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("items[].unitPrice", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Place_OutsideDhaka_ChargesTheOutsideFee()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);

        var response = await PlaceAsync(Phone, Request(Phone, Line(shirt)) with { DeliveryZone = "outsidedhaka" });

        var order = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal(("OutsideDhaka", OutsideDhakaFee, 600m), (order.DeliveryZone, order.DeliveryFee, order.Total));
        Assert.Equal(DeliveryZone.OutsideDhaka, (await LoadOrderAsync(order.Id)).DeliveryZone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Chittagong")]
    [InlineData("1")]
    [InlineData("InsideDhaka,OutsideDhaka")]
    public async Task Place_NotADeliveryZone_ReturnsBadRequestOnTheZoneField(string zone)
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);

        var response = await PlaceAsync(Phone, Request(Phone, Line(shirt)) with { DeliveryZone = zone });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["deliveryZone"], problem!.Errors.Keys);
        Assert.Empty(await OrdersAsync());
    }

    [Fact]
    public async Task Place_UsesTheConfiguredFeeForTheZone()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder
            .UseSetting($"{OrderOptions.SectionName}:{nameof(OrderOptions.InsideDhakaFee)}", "70")
            .UseSetting($"{OrderOptions.SectionName}:{nameof(OrderOptions.OutsideDhakaFee)}", "200"));
        var shirt = Product("Shirt", 450m);
        await SeedAsync(factory, shirt);

        var response = await ClientWith(factory, PhoneToken(factory, Phone))
            .PostAsJsonAsync("/api/orders", Request(Phone, Line(shirt)) with { DeliveryZone = "OutsideDhaka" });

        var order = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal((200m, 650m), (order.DeliveryFee, order.Total));
    }

    [Fact]
    public async Task Order_KeepsTheNameAndPriceItWasPlacedWith()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);
        var placed = await PlaceOkAsync(Phone, Line(shirt));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            var product = await db.Products.SingleAsync(p => p.Id == shirt.Id);
            product.Name = "Renamed Shirt";
            product.Price = 999m;
            await db.SaveChangesAsync();
        }

        var order = await GetAsync<OrderDto>(Phone, $"/api/orders/{placed.Id}");
        Assert.Equal(("Shirt", 450m), (order.Items.Single().ProductName, order.Items.Single().UnitPrice));
        Assert.Equal(530m, order.Total);
    }

    [Fact]
    public async Task ListMine_ReturnsOnlyThisPhonesOrdersNewestFirst()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);
        var first = await PlaceOkAsync(Phone, Line(shirt));
        await PlaceOkAsync(OtherPhone, Line(shirt));
        var second = await PlaceOkAsync(Phone, Line(shirt, quantity: 3));

        var orders = await GetAsync<List<OrderSummaryDto>>(Phone, "/api/orders");

        Assert.Equal([second.Id, first.Id], orders.Select(o => o.Id));
        Assert.Equal(3, orders[0].ItemCount);
        Assert.All(orders, o => Assert.Equal(Phone, o.Phone));
    }

    [Fact]
    public async Task GetOne_AnotherPhonesOrder_ReturnsNotFound()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);
        var theirs = await PlaceOkAsync(OtherPhone, Line(shirt));

        var response = await ClientWith(PhoneToken(Phone)).GetAsync($"/api/orders/{theirs.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutOptions_IsPublicAndListsEachZoneWithItsFee()
    {
        var options = await _factory.CreateClient().GetFromJsonAsync<CheckoutOptionsDto>("/api/checkout/options");

        Assert.Equal(
            [new DeliveryOptionDto("InsideDhaka", InsideDhakaFee), new DeliveryOptionDto("OutsideDhaka", OutsideDhakaFee)],
            options!.DeliveryOptions);
    }

    [Fact]
    public async Task Customer_VerifiesByText_PlacesAnOrder_AndSeesTheStatusTheAdminSet()
    {
        var shirt = Product("Shirt", 450m);
        await SeedAsync(shirt);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/phone/otp", new SendCodeRequest("01712345678"));
        var verify = await client.PostAsJsonAsync(
            "/api/phone/verify", new VerifyCodeRequest("01712345678", _factory.Sms.LastCodeFor(Phone)));
        var verified = (await verify.Content.ReadFromJsonAsync<VerifiedPhoneResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verified.Token);

        var placed = await client.PostAsJsonAsync("/api/orders", Request("01712345678", Line(shirt)));
        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        var order = (await placed.Content.ReadFromJsonAsync<OrderDto>())!;

        (await _factory.CreateAdminClient()
            .PutAsJsonAsync($"/api/admin/orders/{order.Id}/status", new OrderStatusRequest("Confirmed")))
            .EnsureSuccessStatusCode();

        var seen = await client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        Assert.Equal("Confirmed", seen!.Status);
    }

    // The limit keys on the verified phone, not the address, so customers sharing a carrier IP
    // do not use up each other's orders.
    [Fact]
    public async Task Place_PastTheHourlyLimitForAPhone_Returns429_WhileAnotherPhoneCanStillOrder()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimits:OrdersPerPhonePerHour", "1"));
        var shirt = Product("Shirt", 450m);
        await SeedAsync(factory, shirt);

        var first = await PlaceWithAsync(factory, Phone, Request(Phone, Line(shirt)));
        var second = await PlaceWithAsync(factory, Phone, Request(Phone, Line(shirt)));
        var otherPhone = await PlaceWithAsync(factory, OtherPhone, Request(OtherPhone, Line(shirt)));

        Assert.Equal(
            (HttpStatusCode.Created, HttpStatusCode.TooManyRequests, HttpStatusCode.Created),
            (first.StatusCode, second.StatusCode, otherPhone.StatusCode));
        Assert.NotNull(second.Headers.RetryAfter?.Delta);
        Assert.Equal(2, (await OrdersAsync()).Count);
    }

    private static Task<HttpResponseMessage> PlaceWithAsync(
        WebApplicationFactory<Program> factory,
        string verifiedPhone,
        PlaceOrderRequest request) =>
        ClientWith(factory, PhoneToken(factory, verifiedPhone)).PostAsJsonAsync("/api/orders", request);

    private static Product Product(string name, decimal price, params string[] sizes) =>
        Product(name, price, isActive: true, sizes);

    private static Product Product(string name, decimal price, bool isActive, params string[] sizes)
    {
        var product = new Product
        {
            Name = name,
            Slug = Slugs.From(name),
            Price = price,
            CategoryId = 1,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var size in sizes)
        {
            product.Sizes.Add(size);
        }

        return product;
    }

    private static CartQuoteItemRequest Line(Product product, string? size = null, int quantity = 1, decimal? price = null) =>
        new(product.Id, size ?? product.Sizes.FirstOrDefault(), quantity, price ?? product.Price);

    private static PlaceOrderRequest Request(string phone, params CartQuoteItemRequest[] items) =>
        new("Rahim Uddin", phone, "House 12, Road 5, Dhanmondi, Dhaka", null, "InsideDhaka", items);

    private Task SeedAsync(params Product[] products) => SeedAsync(_factory, products);

    private static async Task SeedAsync(WebApplicationFactory<Program> factory, params Product[] products)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private TokenIssuer Issuer() => _factory.Services.GetRequiredService<TokenIssuer>();

    private string PhoneToken(string phone) => PhoneToken(_factory, phone);

    private static string PhoneToken(WebApplicationFactory<Program> factory, string phone) =>
        factory.Services.GetRequiredService<TokenIssuer>().IssueVerifiedPhone(phone, TimeSpan.FromMinutes(30)).Token;

    private HttpClient ClientWith(string token) => ClientWith(_factory, token);

    private static HttpClient ClientWith(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Task<HttpResponseMessage> PlaceAsync(string verifiedPhone, PlaceOrderRequest request) =>
        ClientWith(PhoneToken(verifiedPhone)).PostAsJsonAsync("/api/orders", request);

    private async Task<OrderDto> PlaceOkAsync(string phone, params CartQuoteItemRequest[] items)
    {
        var response = await PlaceAsync(phone, Request(phone, items));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private async Task<T> GetAsync<T>(string phone, string path) =>
        (await ClientWith(PhoneToken(phone)).GetFromJsonAsync<T>(path))!;

    private static async Task<CartQuoteDto> QuoteFromConflictAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("quote")
            .Deserialize<CartQuoteDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private async Task<List<Order>> OrdersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Orders.ToListAsync();
    }

    private async Task<Order> LoadOrderAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Orders.SingleAsync(o => o.Id == id);
    }
}
