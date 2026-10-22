namespace Shop.Api.Security;

/// <summary>Who Google says the holder of an ID token is, once the token has been verified.</summary>
public sealed record GoogleIdentity(string Subject, string Email, string Name);

/// <summary>
/// Verifies a Google ID token. Behind an interface so tests can sign in without calling Google.
/// </summary>
public interface IGoogleTokenVerifier
{
    /// <summary>Returns null for any token that is not a valid, verified sign-in for this app.</summary>
    Task<GoogleIdentity?> VerifyAsync(string idToken);
}
