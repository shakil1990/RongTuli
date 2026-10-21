using Shop.Api.Images;

namespace Shop.Tests;

/// <summary>
/// The signature is the one part of the Cloudinary integration that cannot be tested against the
/// live service without credentials, and a wrong one fails only at upload time, so it is pinned here.
/// </summary>
public sealed class CloudinarySignatureTests
{
    private static readonly Dictionary<string, string> DestroyParameters = new()
    {
        ["public_id"] = "sample_image",
        ["timestamp"] = "1315060510"
    };

    [Fact]
    public void Create_MatchesSha1OfSortedParametersPlusSecret()
    {
        // SHA-1 of "public_id=sample_image&timestamp=1315060510abcd", cross-checked with sha1sum.
        var signature = CloudinarySignature.Create(DestroyParameters, "abcd");

        Assert.Equal("b4ad47fb4e25c7bf5f92a20089f9db59bc302313", signature);
    }

    [Fact]
    public void Create_SortsParametersByName()
    {
        var reversed = new Dictionary<string, string>
        {
            ["timestamp"] = "1315060510",
            ["public_id"] = "sample_image"
        };

        Assert.Equal(
            CloudinarySignature.Create(DestroyParameters, "abcd"),
            CloudinarySignature.Create(reversed, "abcd"));
    }

    [Fact]
    public void Create_DependsOnTheSecret()
    {
        Assert.NotEqual(
            CloudinarySignature.Create(DestroyParameters, "abcd"),
            CloudinarySignature.Create(DestroyParameters, "abce"));
    }
}
