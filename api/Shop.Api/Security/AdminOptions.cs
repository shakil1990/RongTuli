namespace Shop.Api.Security;

/// <summary>
/// The allowlist that decides who is an admin, bound from the "Admin" section:
/// <c>Admin__Emails__0</c>, <c>Admin__Emails__1</c> and so on as host environment variables.
/// Nobody outside this list can reach an admin endpoint, however they signed in.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public IList<string> Emails { get; set; } = [];

    /// <summary>
    /// Google addresses are case-insensitive, and a value pasted into a host's settings form
    /// often carries stray whitespace, so neither is allowed to lock an admin out.
    /// </summary>
    public bool IsAdmin(string email) =>
        Emails.Any(allowed => string.Equals(allowed.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
