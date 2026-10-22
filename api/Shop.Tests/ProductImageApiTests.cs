using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Images;

namespace Shop.Tests;

public sealed class ProductImageApiTests : IDisposable
{
    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UploadTicket_ReturnsSignedParametersAndBrowserLimits()
    {
        var productId = await CreateDraftAsync();

        var ticket = await _factory.CreateAdminClient()
            .PostAsync($"/api/admin/products/{productId}/images/upload-ticket", null)
            .ReadAsync<UploadTicketDto>();

        Assert.Equal(FakeImageStore.Folder, ticket.Folder);
        Assert.Equal("test-signature", ticket.Signature);
        Assert.Equal(5 * 1024 * 1024, ticket.MaxFileBytes);
        Assert.Contains("image/webp", ticket.AllowedContentTypes);
    }

    [Fact]
    public async Task UploadTicket_ForMissingProduct_ReturnsNotFound()
    {
        var response = await _factory.CreateAdminClient()
            .PostAsync("/api/admin/products/999/images/upload-ticket", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ImageEndpoints_WithoutAdmin_ReturnUnauthorized()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateClient();

        var ticket = await client.PostAsync($"/api/admin/products/{productId}/images/upload-ticket", null);
        var add = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1));
        var delete = await client.DeleteAsync($"/api/admin/products/{productId}/images/1");

        Assert.Equal(HttpStatusCode.Unauthorized, ticket.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, add.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task AddImage_StoresUrlAndAppendsToTheEnd()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();

        var first = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1));
        var second = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(2))
            .ReadAsync<ProductImageDto>();
        var product = await client.GetFromJsonAsync<ProductDetailDto>($"/api/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal([0, 1], product!.Images.Select(image => image.SortOrder));
    }

    [Fact]
    public async Task AddImage_SixthImage_ReturnsConflict()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        for (var index = 1; index <= 5; index++)
        {
            await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(index));
        }

        var sixth = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(6));
        var ticket = await client.PostAsync($"/api/admin/products/{productId}/images/upload-ticket", null);

        Assert.Equal(HttpStatusCode.Conflict, sixth.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ticket.StatusCode);
    }

    [Fact]
    public async Task AddImage_SamePublicIdTwice_ReturnsConflict()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1));

        var duplicate = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("", "test/products/a")]
    [InlineData("not-a-url", "test/products/a")]
    [InlineData("https://res.cloudinary.com/test-cloud/image/upload/a.jpg", "")]
    [InlineData("https://res.cloudinary.com/test-cloud/image/upload/a.jpg", "test/products/a\nforged")]
    // A URL and public ID from outside the configured folder are rejected, so a tampered
    // callback cannot point the catalog at someone else's asset.
    [InlineData("https://evil.example.com/a.jpg", "test/products/a")]
    [InlineData("https://res.cloudinary.com/test-cloud/image/upload/a.jpg", "other/folder/a")]
    public async Task AddImage_InvalidPayload_ReturnsBadRequest(string url, string publicId)
    {
        var productId = await CreateDraftAsync();

        var response = await _factory.CreateAdminClient()
            .PostAsJsonAsync($"/api/admin/products/{productId}/images", new AddImageRequest(url, publicId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_RemovesAssetAndClosesTheSortOrderGap()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        var first = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1))
            .ReadAsync<ProductImageDto>();
        await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(2));

        var response = await client.DeleteAsync($"/api/admin/products/{productId}/images/{first.Id}");
        var product = await client.GetFromJsonAsync<ProductDetailDto>($"/api/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["test/products/image-1"], _factory.ImageStore.Deleted);
        Assert.Equal([0], product!.Images.Select(image => image.SortOrder));
    }

    [Fact]
    public async Task DeleteImage_WhenHostUnreachable_KeepsTheRowAndReturnsBadGateway()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        var image = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1))
            .ReadAsync<ProductImageDto>();
        _factory.ImageStore.FailDeletes = true;

        var response = await client.DeleteAsync($"/api/admin/products/{productId}/images/{image.Id}");
        var product = await client.GetFromJsonAsync<ProductDetailDto>($"/api/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Single(product!.Images);
    }

    [Fact]
    public async Task DeleteImage_LastImageOfAnActiveProduct_ReturnsConflict()
    {
        var client = _factory.CreateAdminClient();
        var productId = await CreateDraftAsync();
        var image = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1))
            .ReadAsync<ProductImageDto>();
        await client.PutAsJsonAsync(
            $"/api/admin/products/{productId}", new ProductRequest("Tee", 1, null, 100m, null, true));

        var response = await client.DeleteAsync($"/api/admin/products/{productId}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(_factory.ImageStore.Deleted);
    }

    [Fact]
    public async Task DeleteImage_UnknownImage_ReturnsNotFound()
    {
        var productId = await CreateDraftAsync();

        var response = await _factory.CreateAdminClient()
            .DeleteAsync($"/api/admin/products/{productId}/images/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderImages_SetsTheGivenOrderAndMovesTheCover()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        var first = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1))
            .ReadAsync<ProductImageDto>();
        var second = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(2))
            .ReadAsync<ProductImageDto>();

        var reordered = await client.PutAsJsonAsync(
                $"/api/admin/products/{productId}/images/order",
                new ImageOrderRequest([second.Id, first.Id]))
            .ReadAsync<List<ProductImageDto>>();

        Assert.Equal([second.Id, first.Id], reordered.Select(image => image.Id));
        Assert.Equal([0, 1], reordered.Select(image => image.SortOrder));
    }

    [Fact]
    public async Task ReorderImages_IncompleteList_ReturnsBadRequest()
    {
        var productId = await CreateDraftAsync();
        var client = _factory.CreateAdminClient();
        var image = await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1))
            .ReadAsync<ProductImageDto>();
        await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(2));

        var response = await client.PutAsJsonAsync(
            $"/api/admin/products/{productId}/images/order", new ImageOrderRequest([image.Id]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateProduct_WithoutImages_IsRejectedUntilOneIsAdded()
    {
        var client = _factory.CreateAdminClient();
        var productId = await CreateDraftAsync();
        var activateRequest = new ProductRequest("Tee", 1, null, 100m, null, true);

        var withoutImages = await client.PutAsJsonAsync($"/api/admin/products/{productId}", activateRequest);
        await client.PostAsJsonAsync($"/api/admin/products/{productId}/images", Image(1));
        var withImage = await client.PutAsJsonAsync($"/api/admin/products/{productId}", activateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, withoutImages.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withImage.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AskingForActive_IsRejectedBecauseItCanHaveNoImageYet()
    {
        var response = await _factory.CreateAdminClient().PostAsJsonAsync(
            "/api/admin/products", new ProductRequest("Tee", 1, null, 100m, null, true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicProductDetail_ListsImagesInSortOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var product = new Product
        {
            Name = "Blue Shirt",
            Slug = "blue-shirt",
            Price = 450m,
            CategoryId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Images.Add(new ProductImage { Url = "second.jpg", PublicId = "b", SortOrder = 1 });
        product.Images.Add(new ProductImage { Url = "cover.jpg", PublicId = "a", SortOrder = 0 });
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var detail = await _factory.CreateClient()
            .GetFromJsonAsync<ProductDetailDto>("/api/products/blue-shirt");

        Assert.Equal(["cover.jpg", "second.jpg"], detail!.Images.Select(image => image.Url));
    }

    private static AddImageRequest Image(int index) =>
        new($"{FakeImageStore.UrlPrefix}{FakeImageStore.Folder}/image-{index}.jpg",
            $"{FakeImageStore.Folder}/image-{index}");

    private async Task<int> CreateDraftAsync()
    {
        var created = await _factory.CreateAdminClient()
            .PostAsJsonAsync("/api/admin/products", new ProductRequest("Tee", 1, null, 100m, ["M"], false))
            .ReadAsync<ProductDetailDto>();

        return created.Id;
    }
}

internal static class HttpResponseExtensions
{
    /// <summary>Reads a successful response body, failing the test if the status says otherwise.</summary>
    public static async Task<T> ReadAsync<T>(this Task<HttpResponseMessage> responseTask)
    {
        using var response = await responseTask;
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
