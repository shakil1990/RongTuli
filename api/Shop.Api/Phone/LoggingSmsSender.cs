namespace Shop.Api.Phone;

/// <summary>
/// The development sender: the message goes to the log instead of a phone, so a developer reads
/// the code from the console. This is the one place a code is ever logged, which is why it is only
/// used in Development or when <c>Sms__Provider</c> is set to <c>Log</c> on purpose.
/// </summary>
public sealed partial class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct)
    {
        // Only the last three digits: enough to tell two test phones apart in the console.
        LogMessage(phone[^3..], message);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Development SMS (not sent) to a number ending {PhoneEnding}: {Message}")]
    private partial void LogMessage(string phoneEnding, string message);
}

/// <summary>Stands in when no provider is configured, so the API still starts and only sending fails.</summary>
public sealed class UnconfiguredSmsSender : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct) =>
        throw new InvalidOperationException(
            $"No SMS provider is configured. Set {SmsOptions.SectionName}__{nameof(SmsOptions.Provider)}; " +
            $"'{SmsOptions.LogProvider}' writes codes to the log instead of sending them.");
}
