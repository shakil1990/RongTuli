using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Security;

namespace Shop.Api.Phone;

public enum SendCodeStatus
{
    Sent,
    TooSoon,
    DeliveryFailed
}

/// <summary><see cref="RetryAfter"/> is set when the caller has to wait, <see cref="Response"/> when a code went out.</summary>
public sealed record SendCodeResult(SendCodeStatus Status, SendCodeResponse? Response = null, TimeSpan? RetryAfter = null);

/// <summary>
/// Sends codes and checks them. Every refusal is one of two outcomes — wait, or "invalid or
/// expired" — so a caller learns nothing about which check failed.
/// </summary>
internal sealed partial class PhoneVerificationService(
    ShopDbContext db,
    OtpCodes codes,
    ISmsSender sms,
    TokenIssuer issuer,
    IOptions<OtpOptions> options,
    ILogger<PhoneVerificationService> logger)
{
    private readonly OtpOptions _options = options.Value;

    public async Task<SendCodeResult> SendCodeAsync(string phone, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // At most a day's worth of sends, so this list is never longer than MaxSendsPerDay.
        // Two sends racing past this check can both go out, overshooting a cap by one; the
        // per-IP limit bounds how far that can be pushed.
        var sentAt = await db.PhoneVerifications
            .Where(v => v.Phone == phone && v.CreatedAt > now.AddDays(-1))
            .Select(v => v.CreatedAt)
            .ToListAsync(ct);

        var wait = TimeUntilNextSend(sentAt, now);
        if (wait > TimeSpan.Zero)
        {
            LogSendRefused(wait.TotalSeconds);
            return new SendCodeResult(SendCodeStatus.TooSoon, RetryAfter: wait);
        }

        var code = OtpCodes.Generate();
        var verification = new PhoneVerification
        {
            Phone = phone,
            CodeHash = codes.Hash(phone, code),
            ExpiresAt = now.AddMinutes(_options.TtlMinutes),
            CreatedAt = now
        };
        db.PhoneVerifications.Add(verification);

        // Saved before sending, so a send that fails still counts against the caps: a broken
        // provider must not become a way to retry without limit.
        await db.SaveChangesAsync(ct);

        try
        {
            await sms.SendAsync(
                phone,
                $"Your RongTuli code is {code}. It expires in {_options.TtlMinutes} minutes. Do not share it.",
                ct);
        }
        catch (SmsDeliveryException exception)
        {
            LogDeliveryFailed(exception, verification.Id);
            return new SendCodeResult(SendCodeStatus.DeliveryFailed);
        }

        LogCodeSent(verification.Id);
        return new SendCodeResult(
            SendCodeStatus.Sent,
            new SendCodeResponse(phone, _options.TtlMinutes * 60, _options.ResendCooldownSeconds));
    }

    /// <summary>Returns null for a wrong, expired, used or exhausted code — deliberately one answer.</summary>
    public async Task<VerifiedPhoneResponse?> VerifyAsync(string phone, string code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Only the newest code counts, which is what makes sending a new one retire the old.
        var verification = await db.PhoneVerifications
            .Where(v => v.Phone == phone)
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .FirstOrDefaultAsync(ct);

        if (verification is null
            || verification.ConsumedAt is not null
            || verification.ExpiresAt <= now
            || verification.AttemptCount >= _options.MaxAttempts)
        {
            return null;
        }

        // The attempt is counted whether or not the guess is right, in the same save that consumes
        // a right one. AttemptCount is the concurrency token, so of two guesses racing for the same
        // code only one save wins; the loser is answered as a wrong guess.
        verification.AttemptCount++;
        var isMatch = codes.Matches(phone, code, verification.CodeHash);
        if (isMatch)
        {
            verification.ConsumedAt = now;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            LogAttemptRaced(verification.Id);
            return null;
        }

        if (!isMatch)
        {
            LogWrongCode(verification.Id, verification.AttemptCount);
            return null;
        }

        var token = issuer.IssueVerifiedPhone(phone, TimeSpan.FromMinutes(_options.VerifiedTokenMinutes));
        LogVerified(verification.Id);
        return new VerifiedPhoneResponse(token.Token, token.ExpiresAt, phone);
    }

    /// <summary>The longest of the cooldown and the hourly and daily caps; zero or less when a send may go.</summary>
    private TimeSpan TimeUntilNextSend(List<DateTime> sentAt, DateTime now)
    {
        if (sentAt.Count == 0)
        {
            return TimeSpan.Zero;
        }

        sentAt.Sort();
        var waits = new List<TimeSpan> { sentAt[^1].AddSeconds(_options.ResendCooldownSeconds) - now };

        // A window reopens when the oldest send still filling it ages out.
        var lastHour = sentAt.Where(at => at > now.AddHours(-1)).ToList();
        if (lastHour.Count >= _options.MaxSendsPerHour)
        {
            waits.Add(lastHour[^_options.MaxSendsPerHour].AddHours(1) - now);
        }

        if (sentAt.Count >= _options.MaxSendsPerDay)
        {
            waits.Add(sentAt[^_options.MaxSendsPerDay].AddDays(1) - now);
        }

        return waits.Max();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent phone verification {VerificationId}.")]
    private partial void LogCodeSent(int verificationId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Refused a code send: the phone's limit reopens in {WaitSeconds:0} seconds.")]
    private partial void LogSendRefused(double waitSeconds);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The SMS provider did not take phone verification {VerificationId}.")]
    private partial void LogDeliveryFailed(Exception exception, int verificationId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Wrong code for phone verification {VerificationId} (attempt {AttemptCount}).")]
    private partial void LogWrongCode(int verificationId, int attemptCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Two guesses raced for phone verification {VerificationId}; the later one was refused.")]
    private partial void LogAttemptRaced(int verificationId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Phone verification {VerificationId} succeeded.")]
    private partial void LogVerified(int verificationId);
}
