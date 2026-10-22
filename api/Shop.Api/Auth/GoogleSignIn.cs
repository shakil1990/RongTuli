using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Auth;

public enum SignInStatus
{
    Success,
    InvalidToken,
    NotAnAdmin
}

/// <summary>The outcome of a sign-in attempt; <see cref="Session"/> is set only on success.</summary>
public sealed record SignInResult(SignInStatus Status, SessionResponse? Session)
{
    public static SignInResult Failed(SignInStatus status) => new(status, null);

    public static SignInResult Succeeded(SessionResponse session) => new(SignInStatus.Success, session);
}

/// <summary>
/// Turns a verified Google identity into a RongTuli session: the account is recorded either way,
/// but only an address on the admin allowlist is given a token.
/// </summary>
internal sealed partial class GoogleSignIn(
    IGoogleTokenVerifier verifier,
    TokenIssuer issuer,
    IOptions<AdminOptions> adminOptions,
    ShopDbContext db,
    ILogger<GoogleSignIn> logger)
{
    public async Task<SignInResult> SignInAsync(string idToken, CancellationToken ct)
    {
        var identity = await verifier.VerifyAsync(idToken);
        if (identity is null)
        {
            return SignInResult.Failed(SignInStatus.InvalidToken);
        }

        var user = await UpsertAsync(identity, ct);

        // Signing in is not the same as being allowed in. v1 has nothing for a non-admin to do,
        // so the account is kept — it says who tried — but no token is issued.
        if (user.Role != UserRole.Admin)
        {
            LogRejectedNonAdmin(user.Id);
            return SignInResult.Failed(SignInStatus.NotAnAdmin);
        }

        var token = issuer.Issue(user);
        LogSignedIn(user.Id);

        return SignInResult.Succeeded(
            new SessionResponse(token.Token, token.ExpiresAt, user.Email, user.Name, user.Role.ToString()));
    }

    private async Task<User> UpsertAsync(GoogleIdentity identity, CancellationToken ct)
    {
        // Matched on the Google subject first because it is the one identifier that survives an
        // address change; the email lookup then adopts a row created before such a change.
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSub == identity.Subject, ct)
            ?? await db.Users.FirstOrDefaultAsync(u => u.Email == identity.Email, ct);

        if (user is null)
        {
            user = new User { CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
        }

        user.GoogleSub = identity.Subject;
        user.Email = identity.Email;
        user.Name = identity.Name;

        // The role is re-read from configuration on every sign-in, so removing an address from
        // the allowlist takes effect the next time that person signs in, with no database edit.
        user.Role = adminOptions.Value.IsAdmin(identity.Email) ? UserRole.Admin : UserRole.Viewer;

        await db.SaveChangesAsync(ct);
        return user;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "User {UserId} signed in as an admin.")]
    private partial void LogSignedIn(int userId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "User {UserId} signed in with Google but is not on the admin allowlist; no token issued.")]
    private partial void LogRejectedNonAdmin(int userId);
}
