using System.Globalization;

namespace Shop.Api.Phone;

public static class PhoneEndpoints
{
    public const string SendRateLimit = "otp-send";

    public const string VerifyRateLimit = "otp-verify";

    private const string WrongCodeMessage =
        "That code is wrong or has expired. Check the latest text, or send a new code.";

    public static IEndpointRouteBuilder MapPhoneEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: customers have no account, and this is how they prove a phone is theirs.
        app.MapPost("/api/phone/otp", async (
            SendCodeRequest request,
            PhoneVerificationService verification,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!PhoneNumbers.TryNormalize(request.Phone, out var phone))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["phone"] = [PhoneNumbers.FormatHint]
                });
            }

            var result = await verification.SendCodeAsync(phone, ct);
            return result.Status switch
            {
                SendCodeStatus.Sent => Results.Ok(result.Response),
                SendCodeStatus.TooSoon => TooManyRequests(
                    http,
                    result.RetryAfter!.Value,
                    "A code was sent to this number recently."),
                _ => Results.Problem(
                    "The text message could not be sent just now. Try again in a minute.",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Bad Gateway")
            };
        }).RequireRateLimiting(SendRateLimit);

        app.MapPost("/api/phone/verify", async (
            VerifyCodeRequest request,
            PhoneVerificationService verification,
            CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (!PhoneNumbers.TryNormalize(request.Phone, out var phone))
            {
                errors["phone"] = [PhoneNumbers.FormatHint];
            }

            var code = request.Code?.Trim();
            if (!OtpCodes.IsWellFormed(code))
            {
                errors["code"] = [$"Enter the {OtpCodes.Length}-digit code from the text message."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var verified = await verification.VerifyAsync(phone!, code!, ct);

            // A wrong code is answered as a problem with the code field, so the form shows it there.
            return verified is null
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = [WrongCodeMessage] })
                : Results.Ok(verified);
        }).RequireRateLimiting(VerifyRateLimit);

        return app;
    }

    /// <summary>
    /// A 429 the web app can count down from. The wait goes in the body as well as the header,
    /// since a cross-origin page cannot read Retry-After unless CORS exposes it.
    /// </summary>
    internal static IResult TooManyRequests(HttpContext http, TimeSpan retryAfter, string reason)
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
}
