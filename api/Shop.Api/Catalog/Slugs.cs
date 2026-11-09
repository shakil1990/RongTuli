using System.Globalization;
using System.Text.RegularExpressions;

namespace Shop.Api.Catalog;

public static partial class Slugs
{
    /// <summary>The length of the <c>Category.Slug</c> column.</summary>
    public const int MaxCategorySlugLength = 100;

    /// <summary>The length of the <c>Product.Slug</c> column.</summary>
    public const int MaxProductSlugLength = 200;

    /// <summary>
    /// Lower-case ASCII words joined by hyphens, at most <paramref name="maxLength"/> characters.
    /// Names with no ASCII letters or digits fall back to "item".
    /// </summary>
    public static string From(string name, int maxLength)
    {
        var slug = NonAlphanumeric().Replace(name.ToLowerInvariant(), "-").Trim('-');
        slug = Truncate(slug, maxLength);
        return slug.Length == 0 ? "item" : slug;
    }

    /// <summary>
    /// Appends -2, -3, ... until <paramref name="isTaken"/> reports the slug as free. The base is
    /// shortened to make room for the suffix, so the result always fits a column of
    /// <paramref name="maxLength"/>: a name at the length limit would otherwise overflow it on a
    /// collision, and the database would refuse the row.
    /// </summary>
    public static async Task<string> UniqueAsync(string name, int maxLength, Func<string, Task<bool>> isTaken)
    {
        var baseSlug = From(name, maxLength);
        var candidate = baseSlug;
        for (var suffix = 2; await isTaken(candidate); suffix++)
        {
            var tail = "-" + suffix.ToString(CultureInfo.InvariantCulture);
            candidate = Truncate(baseSlug, maxLength - tail.Length) + tail;
        }

        return candidate;
    }

    // Cutting can leave a hyphen at the end ("blue-" from "blue-shirt"), which is trimmed again.
    private static string Truncate(string slug, int maxLength) =>
        slug.Length <= maxLength ? slug : slug[..maxLength].TrimEnd('-');

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
