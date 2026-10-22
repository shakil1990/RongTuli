namespace Shop.Api.Phone;

/// <summary>
/// Which SMS provider sends the codes, bound from the "Sms" section. The provider has not been
/// chosen yet, so the only value is <see cref="LogProvider"/>, which is also the default in
/// Development. Elsewhere the setting must be given explicitly, so a deployment never writes
/// customers' codes into its logs by accident.
/// </summary>
public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public const string LogProvider = "Log";

    public string? Provider { get; set; }
}
