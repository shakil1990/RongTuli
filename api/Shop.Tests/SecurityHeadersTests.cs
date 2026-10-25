using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Shop.Tests;

public sealed class SecurityHeadersTests : IDisposable
{
    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("/api/categories", HttpStatusCode.OK)]
    [InlineData("/api/no-such-endpoint", HttpStatusCode.NotFound)]
    [InlineData("/api/admin/orders", HttpStatusCode.Unauthorized)]
    public async Task EveryResponse_CarriesTheSecurityHeaders(string path, HttpStatusCode expected)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Development_LeavesOutStrictTransportSecurity()
    {
        var response = await _factory.CreateClient().GetAsync("/api/categories");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Production_SendsStrictTransportSecurity()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var response = await production.CreateClient().GetAsync("/api/categories");

        Assert.Equal("max-age=31536000", Header(response, "Strict-Transport-Security"));
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values)
            ? string.Join(", ", values)
            : null;
}
