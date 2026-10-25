namespace Shop.Api.Phone;

/// <summary>
/// Tuning for phone verification, bound from the "Otp" section (<c>Otp__TtlMinutes</c> and so on).
/// None of it is secret. The send caps are what stand between a script and the SMS credit, so
/// they are deliberately tight; raise them only with the send rate in view.
/// </summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>How long a code can be used after it is sent.</summary>
    public int TtlMinutes { get; set; } = 5;

    /// <summary>Guesses allowed against one code, right or wrong, before it stops being accepted.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>The wait between two sends to the same phone; the web app counts it down.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    public int MaxSendsPerHour { get; set; } = 3;

    public int MaxSendsPerDay { get; set; } = 10;

    /// <summary>
    /// How long a verified phone stays verified. Long enough to fill in an address and confirm,
    /// short enough that a token left in a shared browser is soon useless.
    /// </summary>
    public int VerifiedTokenMinutes { get; set; } = 30;

    /// <summary>
    /// Per client IP, across every phone. Looser than the per-phone caps because mobile carriers
    /// put many customers behind one address.
    /// </summary>
    public int SendsPerIpPerHour { get; set; } = 20;

    public int VerifiesPerIpPerHour { get; set; } = 60;
}
