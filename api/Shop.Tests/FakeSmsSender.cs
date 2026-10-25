using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Shop.Api.Phone;

namespace Shop.Tests;

/// <summary>
/// Catches the texts the API sends, so a test can read the code out of one the way a customer
/// would, and can make the provider fail on demand.
/// </summary>
public sealed partial class FakeSmsSender : ISmsSender
{
    private readonly ConcurrentQueue<(string Phone, string Message)> _sent = new();

    public bool FailNextSend { get; set; }

    public IReadOnlyList<(string Phone, string Message)> Sent => [.. _sent];

    /// <summary>The code in the most recent text to <paramref name="phone"/>.</summary>
    public string LastCodeFor(string phone)
    {
        var message = _sent.Last(sent => sent.Phone == phone).Message;
        return SixDigits().Match(message).Value;
    }

    public Task SendAsync(string phone, string message, CancellationToken ct)
    {
        if (FailNextSend)
        {
            FailNextSend = false;
            throw new SmsDeliveryException("The fake provider was told to fail.");
        }

        _sent.Enqueue((phone, message));
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SixDigits();
}
