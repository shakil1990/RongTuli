namespace Shop.Api.Domain;

/// <summary>
/// A person who signed in with Google. The role is Admin when the email is on the configured allowlist.
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    public string GoogleSub { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Viewer;

    public DateTime CreatedAt { get; set; }
}
