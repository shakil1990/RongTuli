namespace Shop.Api.Security;

/// <summary>
/// The Google OAuth web client this API accepts ID tokens for, bound from the "Google" section:
/// user-secrets locally, <c>Google__ClientId</c> as a host environment variable. The matching
/// browser-side value is <c>VITE_GOOGLE_CLIENT_ID</c>; both must name the same client or every
/// sign-in fails the audience check.
/// </summary>
public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    public string? ClientId { get; set; }
}
