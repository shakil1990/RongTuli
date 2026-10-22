namespace Shop.Api.Phone;

/// <summary>
/// Delivers a text message. Behind an interface so local work, CI and tests need no SMS account
/// and spend no credit; the provider is one implementation chosen by <c>Sms__Provider</c>.
/// </summary>
public interface ISmsSender
{
    /// <summary>Throws <see cref="SmsDeliveryException"/> when the provider refuses or cannot be reached.</summary>
    Task SendAsync(string phone, string message, CancellationToken ct);
}

/// <summary>The SMS provider did not take the message. Never carries the message text.</summary>
public sealed class SmsDeliveryException(string message, Exception? innerException = null)
    : Exception(message, innerException);
