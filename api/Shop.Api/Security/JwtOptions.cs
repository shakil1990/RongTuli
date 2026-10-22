using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Shop.Api.Security;

/// <summary>
/// Settings for the JWT this API issues after a Google sign-in, bound from the "Jwt" section:
/// user-secrets locally, <c>Jwt__Key</c> as a host environment variable.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Both sides of the token are this API, so issuer and audience are fixed.</summary>
    public const string Issuer = "rongtuli-api";

    public const string Audience = "rongtuli-web";

    private const int MinimumKeyBytes = 32;

    public string? Key { get; set; }

    /// <summary>
    /// How long a signed-in admin stays signed in. Long enough to finish a round of product
    /// edits without being interrupted, short enough that a stolen token expires the same day.
    /// There are no refresh tokens: the web app asks for a new Google sign-in instead.
    /// </summary>
    public int LifetimeHours { get; set; } = 8;

    /// <summary>
    /// Reads the configured key, accepting either base64 (what the setup instructions generate)
    /// or raw text, and refuses anything too short to sign an HMAC-SHA256 token safely.
    /// </summary>
    public SymmetricSecurityKey CreateSigningKey()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                $"'{SectionName}:{nameof(Key)}' is not set. Set {SectionName}__{nameof(Key)} " +
                $"(or 'dotnet user-secrets set \"{SectionName}:{nameof(Key)}\" ...' locally) " +
                $"to at least {MinimumKeyBytes} random bytes, base64-encoded.");
        }

        var buffer = new byte[Key.Length];
        var bytes = Convert.TryFromBase64String(Key, buffer, out var written)
            ? buffer[..written]
            : Encoding.UTF8.GetBytes(Key);

        if (bytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:{nameof(Key)}' is only {bytes.Length} bytes. HMAC-SHA256 needs " +
                $"at least {MinimumKeyBytes}; generate one with 'openssl rand -base64 32'.");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
