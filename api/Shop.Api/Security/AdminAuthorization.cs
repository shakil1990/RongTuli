using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Shop.Api.Security;

/// <summary>
/// Authentication and authorization for admin endpoints: a bearer token this API signed itself,
/// carrying the role it decided at sign-in. Anything else answers 401, and a token without the
/// Admin role answers 403.
/// </summary>
public static class AdminAuthorization
{
    public const string Policy = "Admin";
    public const string AdminRole = "Admin";

    public static IServiceCollection AddAdminAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();

        // Unlike the Google client ID and the Cloudinary credentials, the signing key cannot be
        // checked lazily: the bearer scheme needs it to validate the very first request. A missing
        // key therefore stops the API at startup rather than letting it serve tokens it cannot verify.
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = jwtOptions.CreateSigningKey();
        services.AddSingleton(new TokenIssuer(signingKey, jwtOptions.LifetimeHours));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the short JWT names instead of the legacy SOAP claim URIs, so the role
                // claim this API signs is the one the policy below looks for.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = JwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.RequireRole(AdminRole));

        return services;
    }
}
