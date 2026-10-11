using System.Net;
using System.Net.Http.Json;

namespace Shop.Tests;

public sealed class HealthEndpointTests
{
    private sealed record HealthResponse(string Status);

    [Fact]
    public async Task Health_WhenDatabaseReachable_ReturnsOkAndHealthy()
    {
        using var factory = new ShopApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("healthy", body?.Status);
    }

    [Fact]
    public async Task Health_WhenDatabaseUnreachable_ReturnsServiceUnavailableAndUnhealthy()
    {
        using var factory = new UnreachableDatabaseApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("unhealthy", body?.Status);
    }
}
