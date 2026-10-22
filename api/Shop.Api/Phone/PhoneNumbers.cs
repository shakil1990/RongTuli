using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Shop.Api.Phone;

/// <summary>Bangladeshi mobile numbers, the only kind the shop delivers to.</summary>
public static partial class PhoneNumbers
{
    public const string FormatHint = "Enter a Bangladeshi mobile number, like 01712345678.";

    /// <summary>
    /// Accepts the ways people actually write a number — <c>01712345678</c>, <c>+880 1712-345678</c>,
    /// <c>8801712345678</c> — and returns it in E.164 (<c>+8801712345678</c>), so one phone is one
    /// key however it was typed. Operator prefixes run 013 to 019.
    /// </summary>
    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164)
    {
        e164 = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 32)
        {
            return false;
        }

        var compact = Separators().Replace(input.Trim(), string.Empty);
        var match = BangladeshiMobile().Match(compact);
        if (!match.Success)
        {
            return false;
        }

        e164 = "+880" + match.Groups["national"].Value;
        return true;
    }

    [GeneratedRegex(@"[\s\-().]")]
    private static partial Regex Separators();

    // The national number without its trunk 0: 1, an operator digit 3–9, then eight digits.
    [GeneratedRegex(@"^(?:\+?880|0)(?<national>1[3-9][0-9]{8})$")]
    private static partial Regex BangladeshiMobile();
}
