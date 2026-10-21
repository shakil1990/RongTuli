using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Shop.Api.Images;

/// <summary>
/// Cloudinary-backed image host: signs direct browser uploads and deletes assets by public ID.
/// </summary>
internal sealed partial class CloudinaryImageStore : IImageStore
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudinaryImageStore> _logger;
    private readonly CloudinaryOptions _options;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public CloudinaryImageStore(
        HttpClient httpClient,
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryImageStore> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        // Checked here rather than at startup so the API still boots without image credentials;
        // only the image endpoints fail, and with a message that names the missing setting.
        _cloudName = Required(_options.CloudName, nameof(CloudinaryOptions.CloudName));
        _apiKey = Required(_options.ApiKey, nameof(CloudinaryOptions.ApiKey));
        _apiSecret = Required(_options.ApiSecret, nameof(CloudinaryOptions.ApiSecret));
    }

    public UploadTicket CreateUploadTicket()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Only the signed parameters go into the signature, and the browser must send exactly
        // these: a ticket cannot be reused to upload somewhere else in the account.
        var signed = new Dictionary<string, string>
        {
            ["folder"] = _options.Folder,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        return new UploadTicket(
            $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload",
            _apiKey,
            _options.Folder,
            timestamp,
            CloudinarySignature.Create(signed, _apiSecret));
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        var signed = new Dictionary<string, string>
        {
            ["public_id"] = publicId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
        var form = new Dictionary<string, string>(signed)
        {
            ["api_key"] = _apiKey,
            ["signature"] = CloudinarySignature.Create(signed, _apiSecret)
        };

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(
            $"v1_1/{_cloudName}/image/destroy", content, cancellationToken);

        // An asset that is already gone is the outcome a delete wanted anyway.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DestroyResponse>(cancellationToken);
        if (!string.Equals(result?.Result, "ok", StringComparison.Ordinal)
            && !string.Equals(result?.Result, "not found", StringComparison.Ordinal))
        {
            // The row is still removed: a stale asset wastes quota, a kept row shows a dead image.
            // Public IDs pass a strict character check before they are stored, so this cannot forge a log line.
            LogUnexpectedDeleteResult(result?.Result ?? "none", publicId);
        }
    }

    public bool Owns(string url, string publicId) =>
        publicId.StartsWith($"{_options.Folder}/", StringComparison.Ordinal)
        && url.StartsWith($"https://res.cloudinary.com/{_cloudName}/", StringComparison.Ordinal);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Cloudinary setting '{name}' is not set. Set Cloudinary__{name} on the host " +
                $"(or 'dotnet user-secrets set \"Cloudinary:{name}\" ...' locally).")
            : value;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cloudinary reported '{Result}' when deleting image {PublicId}; the asset may still exist.")]
    private partial void LogUnexpectedDeleteResult(string result, string publicId);

    private sealed record DestroyResponse(string? Result);
}
