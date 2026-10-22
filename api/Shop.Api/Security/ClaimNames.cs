namespace Shop.Api.Security;

/// <summary>
/// The claims this API puts in its own tokens. Short JWT names are used as-is on both sides —
/// inbound claim mapping is turned off — so what is signed is what the policy reads.
/// </summary>
public static class ClaimNames
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string Name = "name";

    public const string Role = "role";
}
