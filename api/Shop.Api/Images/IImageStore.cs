namespace Shop.Api.Images;

/// <summary>
/// The image host. Behind an interface so the tests — and CI — run without Cloudinary credentials.
/// </summary>
public interface IImageStore
{
    /// <summary>Signs a single direct upload into the configured folder.</summary>
    UploadTicket CreateUploadTicket();

    /// <summary>Deletes the stored asset. An asset that is already gone counts as deleted.</summary>
    Task DeleteAsync(string publicId, CancellationToken cancellationToken);

    /// <summary>
    /// True when the URL and public ID really belong to this account's upload folder. The browser
    /// reports both after uploading, so they are checked before the API stores or deletes them.
    /// </summary>
    bool Owns(string url, string publicId);
}
