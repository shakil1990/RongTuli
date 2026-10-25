namespace Shop.Api.Security;

/// <summary>
/// Request limits for the endpoints outside phone verification, bound from the "RateLimits"
/// section (<c>RateLimits__SignInsPerIpPerHour</c> and so on). None of it is secret. The OTP
/// limits stay in <see cref="Phone.OtpOptions"/>, beside the per-phone caps they work with.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    /// <summary>
    /// Admin sign-ins per client IP. Two admins sign in a few times a day; this only stops a
    /// script from making the API verify token after token.
    /// </summary>
    public int SignInsPerIpPerHour { get; set; } = 20;

    /// <summary>
    /// Orders per verified phone. A real customer places one or two; anything past this is a
    /// script holding a verified token, and every order it places is a wasted delivery.
    /// </summary>
    public int OrdersPerPhonePerHour { get; set; } = 10;
}
