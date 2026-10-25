using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Api.Phone;

namespace Shop.Api.Security;

/// <summary>
/// Every rate limit the API applies, in one place so the policies and the 429 they answer with
/// stay consistent. Endpoints opt in with <c>RequireRateLimiting</c> and one of the names below.
/// </summary>
public static class RateLimits
{
    public const string OtpSend = "otp-send";

    public const string OtpVerify = "otp-verify";

    public const string SignIn = "sign-in";

    public const string PlaceOrder = "place-order";

    public static IServiceCollection AddRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));

        var otp = configuration.GetSection(OtpOptions.SectionName).Get<OtpOptions>() ?? new OtpOptions();
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
            ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(OtpSend, http => PerClientIp(http, otp.SendsPerIpPerHour));
            limiter.AddPolicy(OtpVerify, http => PerClientIp(http, otp.VerifiesPerIpPerHour));
            limiter.AddPolicy(SignIn, http => PerClientIp(http, limits.SignInsPerIpPerHour));
            limiter.AddPolicy(PlaceOrder, http => PerVerifiedPhone(http, limits.OrdersPerPhonePerHour));
            limiter.OnRejected = async (context, ct) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait)
                    ? wait
                    : TimeSpan.FromHours(1);
                await TooManyRequests(context.HttpContext, retryAfter, "Too many requests.")
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

    /// <summary>
    /// A 429 the web app can count down from. The wait goes in the body as well as the header,
    /// since a cross-origin page cannot read Retry-After unless CORS exposes it.
    /// </summary>
    public static IResult TooManyRequests(HttpContext http, TimeSpan retryAfter, string reason)
    {
        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        return Results.Problem(
            $"{reason} Try again in {DescribeWait(seconds)}.",
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            extensions: new Dictionary<string, object?> { ["retryAfterSeconds"] = seconds });
    }

    private static string DescribeWait(int seconds) => seconds switch
    {
        <= 90 => $"{seconds} seconds",
        < 90 * 60 => $"{(int)Math.Ceiling(seconds / 60.0)} minutes",
        _ => $"{(int)Math.Ceiling(seconds / 3600.0)} hours"
    };

    private static RateLimitPartition<string> PerClientIp(HttpContext http, int permitsPerHour) =>
        HourlyWindow("ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "unknown"), permitsPerHour);

    // The limiter runs after authorization, so the policy on the endpoint has already required the
    // claim. Keying on the phone rather than the address keeps customers behind one carrier IP apart.
    private static RateLimitPartition<string> PerVerifiedPhone(HttpContext http, int permitsPerHour) =>
        http.User.FindFirstValue(ClaimNames.Phone) is { } phone
            ? HourlyWindow("phone:" + phone, permitsPerHour)
            : PerClientIp(http, permitsPerHour);

    private static RateLimitPartition<string> HourlyWindow(string key, int permitsPerHour) =>
        RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
}
