using System.Diagnostics.CodeAnalysis;

namespace Shop.Api.Orders;

/// <summary>Reads enums from request text by name only.</summary>
public static class EnumNames
{
    /// <summary>
    /// Reads a member by name, ignoring case. A number or a comma list is refused even though
    /// <c>Enum.TryParse</c> would take either, since "3" or "Pending,Shipped" says nothing a person can check.
    /// </summary>
    public static bool TryParse<TEnum>(string? value, [NotNullWhen(true)] out TEnum? result)
        where TEnum : struct, Enum
    {
        result = null;
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name) || !name.All(char.IsLetter)
            || !Enum.TryParse<TEnum>(name, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        result = parsed;
        return true;
    }

    public static string List<TEnum>()
        where TEnum : struct, Enum => string.Join(", ", Enum.GetNames<TEnum>());
}
