using System.Text;
using System.Text.RegularExpressions;

namespace Shop.Api.Catalog;

public static partial class Slugs
{
    /// <summary>
    /// Lower-case ASCII words joined by hyphens. Names with no ASCII letters or digits fall back to "item".
    /// </summary>
    public static string From(string name)
    {
        var slug = NonAlphanumeric().Replace(name.ToLowerInvariant(), "-").Trim('-');
        return slug.Length == 0 ? "item" : slug;
    }

    /// <summary>
    /// Appends -2, -3, ... until <paramref name="isTaken"/> reports the slug as free.
    /// </summary>
    public static async Task<string> UniqueAsync(string name, Func<string, Task<bool>> isTaken)
    {
        var baseSlug = From(name);
        var candidate = baseSlug;
        for (var suffix = 2; await isTaken(candidate); suffix++)
        {
            candidate = new StringBuilder(baseSlug).Append('-').Append(suffix).ToString();
        }

        return candidate;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
