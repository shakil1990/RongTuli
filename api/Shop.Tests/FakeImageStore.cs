using Shop.Api.Images;

namespace Shop.Tests;

/// <summary>
/// Stands in for Cloudinary so the tests need no account and no network. Records what was deleted
/// and can be told to fail, which is how the unreachable-host path is exercised.
/// </summary>
public sealed class FakeImageStore : IImageStore
{
    public const string Folder = "test/products";
    public const string UrlPrefix = "https://res.cloudinary.com/test-cloud/image/upload/";

    public List<string> Deleted { get; } = [];

    public bool FailDeletes { get; set; }

    public UploadTicket CreateUploadTicket() =>
        new("https://api.cloudinary.com/v1_1/test-cloud/image/upload", "test-key", Folder, 1_700_000_000, "test-signature");

    public Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        if (FailDeletes)
        {
            throw new HttpRequestException("The image host is unreachable.");
        }

        Deleted.Add(publicId);
        return Task.CompletedTask;
    }

    public bool Owns(string url, string publicId) =>
        publicId.StartsWith($"{Folder}/", StringComparison.Ordinal)
        && url.StartsWith(UrlPrefix, StringComparison.Ordinal);
}
