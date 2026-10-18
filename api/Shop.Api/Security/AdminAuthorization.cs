using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Shop.Api.Security;

/// <summary>
/// Authorization for admin endpoints. Until Google sign-in lands (Step 3) no scheme can authenticate
/// anyone, so every admin endpoint answers 401. Step 3 replaces the placeholder scheme with JWT bearer.
/// </summary>
public static class AdminAuthorization
{
    public const string Policy = "Admin";
    public const string AdminRole = "Admin";

    private const string PlaceholderScheme = "Unconfigured";

    public static IServiceCollection AddAdminAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(PlaceholderScheme)
            .AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(PlaceholderScheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.RequireRole(AdminRole));

        return services;
    }

    private sealed class UnconfiguredAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
