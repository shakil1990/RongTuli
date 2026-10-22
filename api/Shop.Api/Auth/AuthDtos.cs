namespace Shop.Api.Auth;

/// <summary>The ID token the Google sign-in button handed the browser.</summary>
public sealed record GoogleSignInRequest(string? IdToken);

/// <summary>
/// A signed-in admin session. The web app stores the token and sends it as a bearer header;
/// the rest is what the header needs to show who is signed in.
/// </summary>
public sealed record SessionResponse(string Token, DateTimeOffset ExpiresAt, string Email, string Name, string Role);

/// <summary>Who the caller is, according to the token they just sent.</summary>
public sealed record MeResponse(string Email, string Name, string Role);
