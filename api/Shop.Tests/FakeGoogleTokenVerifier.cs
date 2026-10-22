using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// Stands in for Google so the tests need no network and no real ID token. A token is valid only
/// if a test registered it; anything else comes back unverified, exactly as a forged one would.
/// </summary>
public sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly Dictionary<string, GoogleIdentity> _identities = [];

    /// <summary>Makes <paramref name="idToken"/> verify as the given Google account.</summary>
    public string Accept(string idToken, string subject, string email, string name = "Test Person")
    {
        _identities[idToken] = new GoogleIdentity(subject, email, name);
        return idToken;
    }

    public Task<GoogleIdentity?> VerifyAsync(string idToken) =>
        Task.FromResult(_identities.GetValueOrDefault(idToken));
}
