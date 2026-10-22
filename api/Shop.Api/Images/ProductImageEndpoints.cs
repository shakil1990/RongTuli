using Microsoft.EntityFrameworkCore;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Images;

/// <summary>
/// Admin image management. The browser uploads the file straight to the image host with a signed
/// ticket and then tells the API where it landed, so no image bytes pass through this service.
/// </summary>
public static class ProductImageEndpoints
{
    public static IEndpointRouteBuilder MapProductImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/products/{productId:int}/images")
            .RequireAuthorization(AdminAuthorization.Policy);

        group.MapPost("/upload-ticket", async (
            int productId,
            ShopDbContext db,
            IImageStore store,
            CancellationToken ct) =>
        {
            if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            {
                return Results.NotFound();
            }

            // Checked before signing as well as on save, so a product that is already full is
            // told so before an upload burns the account's bandwidth.
            if (await db.ProductImages.CountAsync(i => i.ProductId == productId, ct)
                >= ImageValidation.MaxImagesPerProduct)
            {
                return TooManyImages();
            }

            var ticket = store.CreateUploadTicket();

            return Results.Ok(new UploadTicketDto(
                ticket.UploadUrl,
                ticket.ApiKey,
                ticket.Folder,
                ticket.Timestamp,
                ticket.Signature,
                ImageValidation.MaxFileBytes,
                ImageValidation.AllowedContentTypes));
        });

        group.MapPost("/", async (
            int productId,
            AddImageRequest request,
            ShopDbContext db,
            IImageStore store,
            CancellationToken ct) =>
        {
            var errors = ImageValidation.Validate(request, store);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var product = await LoadAsync(db, productId, ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            if (product.Images.Count >= ImageValidation.MaxImagesPerProduct)
            {
                return TooManyImages();
            }

            var publicId = request.PublicId!.Trim();
            if (await db.ProductImages.AnyAsync(i => i.PublicId == publicId, ct))
            {
                return Conflict("This image is already attached to a product.");
            }

            // Sort orders stay a gapless sequence from 0, so a new image goes on the end.
            var image = new ProductImage
            {
                Url = request.Url!.Trim(),
                PublicId = publicId,
                SortOrder = product.Images.Count
            };
            product.Images.Add(image);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/products/{productId}/images/{image.Id}", ToDto(image));
        });

        group.MapDelete("/{imageId:int}", async (
            int productId,
            int imageId,
            ShopDbContext db,
            IImageStore store,
            CancellationToken ct) =>
        {
            var product = await LoadAsync(db, productId, ct);
            var image = product?.Images.FirstOrDefault(i => i.Id == imageId);
            if (product is null || image is null)
            {
                return Results.NotFound();
            }

            if (product.IsActive && product.Images.Count == 1)
            {
                return Conflict(
                    "An active product needs at least one image. Upload a replacement first, or deactivate the product.");
            }

            // The asset goes first: if the host is unreachable nothing changes and the admin can
            // retry, where the other order would leave a row pointing at a deleted image.
            try
            {
                await store.DeleteAsync(image.PublicId, ct);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return Results.Problem(
                    "The image host could not be reached, so nothing was deleted. Please try again.",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Image host unavailable");
            }

            product.Images.Remove(image);
            Resequence(product);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        // A full replacement of the order, so the body lists every image of the product.
        group.MapPut("/order", async (
            int productId,
            ImageOrderRequest request,
            ShopDbContext db,
            CancellationToken ct) =>
        {
            var product = await LoadAsync(db, productId, ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            var imageIds = request.ImageIds ?? [];
            if (imageIds.Count != product.Images.Count
                || imageIds.Distinct().Count() != imageIds.Count
                || imageIds.Any(id => product.Images.All(image => image.Id != id)))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["imageIds"] = ["List every image of this product exactly once."]
                });
            }

            for (var index = 0; index < imageIds.Count; index++)
            {
                product.Images.First(image => image.Id == imageIds[index]).SortOrder = index;
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(product.Images.OrderBy(image => image.SortOrder).Select(ToDto).ToList());
        });

        return app;
    }

    private static Task<Product?> LoadAsync(ShopDbContext db, int productId, CancellationToken ct) =>
        db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct);

    /// <summary>Closes the gap a removal leaves, so sort order 0 is always the cover.</summary>
    private static void Resequence(Product product)
    {
        var order = 0;
        foreach (var image in product.Images.OrderBy(image => image.SortOrder).ToList())
        {
            image.SortOrder = order++;
        }
    }

    private static ProductImageDto ToDto(ProductImage image) => new(image.Id, image.Url, image.SortOrder);

    private static IResult TooManyImages() =>
        Conflict($"A product can have at most {ImageValidation.MaxImagesPerProduct} images.");

    private static IResult Conflict(string detail) =>
        Results.Problem(detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");
}
