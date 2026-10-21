using System.Text.RegularExpressions;

namespace Shop.Api.Images;

internal static partial class ImageValidation
{
    public const int MaxImagesPerProduct = 5;

    /// <summary>Upload limit checked in the browser and signalled to it by the upload ticket.</summary>
    public const long MaxFileBytes = 5L * 1024 * 1024;

    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private const int MaxUrlLength = 2000;

    public static Dictionary<string, string[]> Validate(AddImageRequest request, IImageStore store)
    {
        var errors = new Dictionary<string, string[]>();
        var url = request.Url?.Trim() ?? string.Empty;
        var publicId = request.PublicId?.Trim() ?? string.Empty;

        if (url.Length is 0 or > MaxUrlLength || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            errors["url"] = [$"A valid absolute image URL of at most {MaxUrlLength} characters is required."];
        }

        if (!PublicIdPattern().IsMatch(publicId))
        {
            errors["publicId"] = ["Public ID is required and may contain only letters, digits and the _ - . / characters."];
        }

        // The browser says where it uploaded, so the pair is only trusted once it is confirmed to
        // sit in this account's own folder. The character check also keeps control characters,
        // and so forged log lines, out of a stored public ID.
        if (errors.Count == 0 && !store.Owns(url, publicId))
        {
            errors["publicId"] = ["This URL and public ID do not belong to the configured upload folder."];
        }

        return errors;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-./]{1,500}$")]
    private static partial Regex PublicIdPattern();
}
