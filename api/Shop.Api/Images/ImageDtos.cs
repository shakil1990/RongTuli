namespace Shop.Api.Images;

/// <summary>Signed parameters that let the browser upload one file straight to the image host.</summary>
public sealed record UploadTicket(string UploadUrl, string ApiKey, string Folder, long Timestamp, string Signature);

/// <summary>
/// An upload ticket plus the limits the browser checks a file against before it starts uploading,
/// so the rules live on the server only.
/// </summary>
public sealed record UploadTicketDto(
    string UploadUrl,
    string ApiKey,
    string Folder,
    long Timestamp,
    string Signature,
    long MaxFileBytes,
    IReadOnlyList<string> AllowedContentTypes);

/// <summary>What the browser reports back once the image host has accepted the file.</summary>
public sealed record AddImageRequest(string? Url, string? PublicId);

public sealed record ImageOrderRequest(IReadOnlyList<int>? ImageIds);
