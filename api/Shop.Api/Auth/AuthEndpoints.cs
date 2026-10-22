using System.Security.Claims;
using Shop.Api.Security;

namespace Shop.Api.Auth;

public static class AuthEndpoints
{
    /// <summary>A Google ID token is about a kilobyte; well past that it is not worth verifying.</summary>
    private const int MaxIdTokenLength = 4096;

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/google", async (
            GoogleSignInRequest request,
            GoogleSignIn signIn,
            CancellationToken ct) =>
        {
            var idToken = request.IdToken?.Trim() ?? string.Empty;
            if (idToken.Length is 0 or > MaxIdTokenLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["idToken"] = [$"A Google ID token of at most {MaxIdTokenLength} characters is required."]
                });
            }

            var result = await signIn.SignInAsync(idToken, ct);

            // The two failures are deliberately told apart: the browser can retry a stale token
            // silently, but an address that is not an admin has to be shown to the person.
            return result.Status switch
            {
                SignInStatus.Success => Results.Ok(result.Session),
                SignInStatus.NotAnAdmin => Results.Problem(
                    "This Google account is not a RongTuli admin.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden"),
                _ => Results.Problem(
                    "This Google sign-in could not be verified. Try signing in again.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized")
            };
        });

        app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new MeResponse(
                user.FindFirstValue(ClaimNames.Email) ?? string.Empty,
                user.FindFirstValue(ClaimNames.Name) ?? string.Empty,
                user.FindFirstValue(ClaimNames.Role) ?? string.Empty)))
            .RequireAuthorization(AdminAuthorization.Policy);

        return app;
    }
}
