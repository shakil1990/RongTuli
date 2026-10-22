namespace Shop.Api.Images;

/// <summary>
/// Cloudinary account settings, bound from the "Cloudinary" configuration section:
/// user-secrets locally, <c>Cloudinary__CloudName</c> and friends as host environment variables.
/// </summary>
public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; set; }

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    /// <summary>
    /// The folder every upload lands in. The free plan has one environment, so local work points
    /// this at its own prefix instead of mixing development uploads into the production folder.
    /// </summary>
    public string Folder { get; set; } = "rongtuli/products";
}
