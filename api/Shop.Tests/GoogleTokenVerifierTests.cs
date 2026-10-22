using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// Covers the real verifier, which the API tests replace with a fake. Every token below is
/// malformed badly enough to be rejected while it is still being parsed, so these tests reach
/// no further than the library and need no network.
/// </summary>
public sealed class GoogleTokenVerifierTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("only.two")]
    [InlineData("!!!.???.***")]
    [InlineData("eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxIn0.bogus")]
    public async Task Verify_MalformedToken_ReturnsNullRatherThanThrowing(string idToken)
    {
        var verifier = CreateVerifier();

        var identity = await verifier.VerifyAsync(idToken);

        Assert.Null(identity);
    }

    [Fact]
    public void Construct_WithoutAClientId_SaysWhichSettingIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateVerifier(clientId: null));

        Assert.Contains(nameof(GoogleOptions.ClientId), exception.Message, StringComparison.Ordinal);
    }

    private static IGoogleTokenVerifier CreateVerifier(string? clientId = "test-client-id") =>
        new GoogleTokenVerifier(
            Options.Create(new GoogleOptions { ClientId = clientId }),
            NullLogger<GoogleTokenVerifier>.Instance);
}
