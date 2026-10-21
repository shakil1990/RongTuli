using System.Security.Cryptography;
using System.Text;

namespace Shop.Api.Images;

/// <summary>
/// Cloudinary's request signature: the signed parameters sorted by name and joined as a query
/// string, the API secret appended, then SHA-1 as lower-case hex. SHA-1 is what the Cloudinary
/// API requires for this signature; it is not a security choice of ours.
/// </summary>
public static class CloudinarySignature
{
    public static string Create(IReadOnlyDictionary<string, string> parameters, string apiSecret)
    {
        var payload = string.Join(
            '&',
            parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => $"{parameter.Key}={parameter.Value}"));

        return Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(payload + apiSecret)));
    }
}
