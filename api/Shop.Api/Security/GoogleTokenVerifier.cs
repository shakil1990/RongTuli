using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Shop.Api.Security;

/// <summary>
/// Verifies ID tokens against Google's published signing keys, which the library fetches and caches.
/// </summary>
internal sealed partial class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly ILogger<GoogleTokenVerifier> _logger;
    private readonly string _clientId;

    public GoogleTokenVerifier(IOptions<GoogleOptions> options, ILogger<GoogleTokenVerifier> logger)
    {
        _logger = logger;

        // Checked here rather than at startup so the API still boots without a Google client;
        // only sign-in fails, and with a message that names the missing setting.
        _clientId = options.Value.ClientId is { Length: > 0 } clientId
            ? clientId
            : throw new InvalidOperationException(
                $"'{GoogleOptions.SectionName}:{nameof(GoogleOptions.ClientId)}' is not set. Set " +
                $"{GoogleOptions.SectionName}__{nameof(GoogleOptions.ClientId)} to the Google OAuth " +
                "web client ID, the same value the web app uses as VITE_GOOGLE_CLIENT_ID.");
    }

    public async Task<GoogleIdentity?> VerifyAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Pinning the audience is what stops an ID token minted for some other site being
            // replayed here; the library checks the issuer, signature and expiry itself.
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] });
        }
        catch (InvalidJwtException exception)
        {
            LogRejectedToken(exception);
            return null;
        }

        // An unverified address could be anyone's, so it is not allowed to match the allowlist.
        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
        {
            LogUnusablePayload(payload.EmailVerified);
            return null;
        }

        var email = payload.Email.Trim();
        var name = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name.Trim();
        return new GoogleIdentity(payload.Subject.Trim(), email, name);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Google rejected a sign-in token.")]
    private partial void LogRejectedToken(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "A Google token verified but carried no usable identity (emailVerified={EmailVerified}).")]
    private partial void LogUnusablePayload(bool emailVerified);
}
