using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Shop.Api.Phone;

/// <summary>
/// Makes and checks one-time codes. Codes are stored only as an HMAC, keyed with a key derived
/// from the JWT signing key: a copy of the table alone cannot be brute-forced back to live codes,
/// and the derived key never signs anything else.
/// </summary>
public sealed class OtpCodes
{
    public const int Length = 6;

    private static readonly byte[] KeyPurpose = Encoding.UTF8.GetBytes("rongtuli/otp-code-hash/v1");

    private readonly byte[] _hashKey;

    public OtpCodes(byte[] signingKey)
    {
        _hashKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, signingKey, outputLength: 32, info: KeyPurpose);
    }

    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    /// <summary>True for exactly six ASCII digits — the only shape a code can have.</summary>
    public static bool IsWellFormed(string? code) =>
        code is { Length: Length } && code.All(char.IsAsciiDigit);

    /// <summary>
    /// The phone is part of the hash so a code is only ever valid for the number it was sent to,
    /// even if two numbers happen to draw the same six digits.
    /// </summary>
    public string Hash(string phone, string code) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_hashKey, Encoding.UTF8.GetBytes($"{phone}:{code}")));

    /// <summary>Compares in constant time so the response time says nothing about how close a guess was.</summary>
    public bool Matches(string phone, string code, string storedHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(phone, code)),
            Encoding.ASCII.GetBytes(storedHash));
}
