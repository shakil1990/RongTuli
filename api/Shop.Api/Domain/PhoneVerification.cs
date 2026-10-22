namespace Shop.Api.Domain;

/// <summary>
/// One code sent to one phone. A row is written for every send, so the rows for a phone in the
/// last hour or day are also what the send caps count. Only the newest row for a phone can be
/// verified; sending a new code therefore retires the previous one.
/// </summary>
public sealed class PhoneVerification
{
    public int Id { get; set; }

    /// <summary>E.164, e.g. <c>+8801712345678</c>.</summary>
    public string Phone { get; set; } = null!;

    /// <summary>
    /// HMAC-SHA256 of the phone and code, hex-encoded. The code itself is never stored.
    /// </summary>
    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Wrong and right guesses alike. Also the concurrency token, so two guesses racing for the
    /// last attempt cannot both be counted as one.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>Set once the code has been accepted; a consumed code is never accepted again.</summary>
    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
