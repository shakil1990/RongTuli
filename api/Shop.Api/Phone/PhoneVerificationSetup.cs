using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Api.Security;

namespace Shop.Api.Phone;

public static class PhoneVerificationSetup
{
    public static IServiceCollection AddPhoneVerification(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<PhoneVerificationService>();
        services.AddHostedService<PhoneVerificationPruner>();

        // The signing key was already checked by AddAdminAuthorization; read here only to derive
        // the separate key the code hashes use.
        var signingKey = (configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions())
            .CreateSigningKey();
        services.AddSingleton(new OtpCodes(signingKey.Key));

        var provider = configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>()?.Provider?.Trim();
        if (string.IsNullOrEmpty(provider) && environment.IsDevelopment())
        {
            provider = SmsOptions.LogProvider;
        }

        if (string.Equals(provider, SmsOptions.LogProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
        }
        else
        {
            services.AddSingleton<ISmsSender, UnconfiguredSmsSender>();
        }

        var otp = configuration.GetSection(OtpOptions.SectionName).Get<OtpOptions>() ?? new OtpOptions();
        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(PhoneEndpoints.SendRateLimit, http => PerClientIp(http, otp.SendsPerIpPerHour));
            limiter.AddPolicy(PhoneEndpoints.VerifyRateLimit, http => PerClientIp(http, otp.VerifiesPerIpPerHour));
            limiter.OnRejected = async (context, ct) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait)
                    ? wait
                    : TimeSpan.FromHours(1);
                await PhoneEndpoints
                    .TooManyRequests(context.HttpContext, retryAfter, "Too many requests from this network.")
                    .ExecuteAsync(context.HttpContext);
            };
        });

        // The host's proxy appends the caller's address to X-Forwarded-For; without reading it,
        // every customer would share the proxy's address and one per-IP limit. Only the last entry
        // is taken — the one the proxy itself added — because anything before it came from the
        // caller and can be forged. The proxy's own address is not fixed, so none is pinned.
        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            forwarded.ForwardLimit = 1;
            forwarded.KnownIPNetworks.Clear();
            forwarded.KnownProxies.Clear();
        });

        return services;
    }

    private static RateLimitPartition<string> PerClientIp(HttpContext http, int permitsPerHour) =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
}
