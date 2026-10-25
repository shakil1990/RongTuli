using System.Globalization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shop.Api.Domain;

namespace Shop.Api.Security;

/// <summary>A signed session token and the moment the web app should stop using it.</summary>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues this API's own tokens. A Google ID token only proves who someone is once, at sign-in;
/// every request after that carries one of these instead.
/// </summary>
public sealed class TokenIssuer(SymmetricSecurityKey signingKey, int lifetimeHours)
{
    public IssuedToken Issue(User user) => Create(
        TimeSpan.FromHours(lifetimeHours),
        new Dictionary<string, object>
        {
            [ClaimNames.Subject] = user.Id.ToString(CultureInfo.InvariantCulture),
            [ClaimNames.Email] = user.Email,
            [ClaimNames.Name] = user.Name,
            [ClaimNames.Role] = user.Role.ToString()
        });

    /// <summary>
    /// Proof that the holder received a code on <paramref name="phone"/>. It carries no role, so the
    /// admin policy refuses it like any other non-admin token.
    /// </summary>
    public IssuedToken IssueVerifiedPhone(string phone, TimeSpan lifetime) => Create(
        lifetime,
        new Dictionary<string, object> { [ClaimNames.Phone] = phone });

    private IssuedToken Create(TimeSpan lifetime, Dictionary<string, object> claims)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(lifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
