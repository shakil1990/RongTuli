namespace Shop.Api.Security;

/// <summary>
/// Checks for characters no person types into a form. A control character in a name would be
/// stored, shown to an admin and copied onto orders, and it can break a log line or a CSV export,
/// so free text is refused rather than cleaned up silently.
/// </summary>
public static class TextInput
{
    /// <summary>A name, a size or anything else typed into a one-line field: no control characters at all.</summary>
    public static bool IsSingleLine(string? value) => value is null || !value.Any(char.IsControl);

    /// <summary>An address, a note or a description: line breaks and tabs are fine, other control characters are not.</summary>
    public static bool IsMultiLine(string? value) =>
        value is null || !value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
}
