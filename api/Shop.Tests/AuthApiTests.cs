using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Auth;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Tests;

/// <summary>
/// Runs against the real bearer scheme rather than the stub header, so a test signs in and then
/// uses the token it was issued — the same round trip the live site makes.
/// </summary>
public sealed class AuthApiTests : IDisposable
{
    private const string OutsiderEmail = "someone.else@example.com";

    private readonly ShopApiFactory _factory = new(useRealAuthentication: true);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SignIn_AllowlistedAccount_ReturnsTokenAndAdminRole()
    {
        _factory.GoogleTokens.Accept("admin-token", "google-sub-1", TestSettings.AdminEmail, "Shop Owner");

        var session = await SignInAsync("admin-token");

        Assert.Equal((TestSettings.AdminEmail, "Shop Owner", "Admin"), (session.Email, session.Name, session.Role));
        Assert.NotEmpty(session.Token);
        Assert.True(session.ExpiresAt > DateTimeOffset.UtcNow.AddHours(7));
    }

    [Fact]
    public async Task SignIn_AccountNotOnAllowlist_ReturnsForbidden()
    {
        _factory.GoogleTokens.Accept("outsider-token", "google-sub-2", OutsiderEmail);

        var response = await PostSignInAsync("outsider-token");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_AccountNotOnAllowlist_IsStillRecordedAsAViewer()
    {
        _factory.GoogleTokens.Accept("outsider-token", "google-sub-2", OutsiderEmail);

        await PostSignInAsync("outsider-token");

        var user = await FindUserAsync(OutsiderEmail);
        Assert.Equal(UserRole.Viewer, user.Role);
    }

    [Fact]
    public async Task SignIn_TokenGoogleDoesNotVerify_ReturnsUnauthorized()
    {
        var response = await PostSignInAsync("forged-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_MissingIdToken_ReturnsBadRequest()
    {
        var response = await PostSignInAsync(idToken: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // The allowlist is typed into a host settings form by hand, so an entry that arrives with
    // different capitalisation or stray spaces must not quietly lock that admin out.
    [Fact]
    public async Task SignIn_AllowlistEntryWithOddCaseAndSpacing_StillGrantsAdmin()
    {
        _factory.GoogleTokens.Accept("second-admin-token", "google-sub-3", TestSettings.SecondAdminEmail);

        var session = await SignInAsync("second-admin-token");

        Assert.Equal("Admin", session.Role);
    }

    [Fact]
    public async Task SignIn_SameGoogleAccountTwice_KeepsOneUserRow()
    {
        _factory.GoogleTokens.Accept("admin-token", "google-sub-1", TestSettings.AdminEmail);

        await SignInAsync("admin-token");
        await SignInAsync("admin-token");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Single(db.Users.Where(u => u.Email == TestSettings.AdminEmail));
    }

    [Fact]
    public async Task SignIn_AddressMovedToANewGoogleAccount_AdoptsTheExistingRow()
    {
        _factory.GoogleTokens.Accept("first-token", "google-sub-old", TestSettings.AdminEmail);
        _factory.GoogleTokens.Accept("second-token", "google-sub-new", TestSettings.AdminEmail);

        await SignInAsync("first-token");
        await SignInAsync("second-token");

        var user = await FindUserAsync(TestSettings.AdminEmail);
        Assert.Equal("google-sub-new", user.GoogleSub);
    }

    [Fact]
    public async Task Me_WithIssuedToken_ReturnsTheSignedInAdmin()
    {
        _factory.GoogleTokens.Accept("admin-token", "google-sub-1", TestSettings.AdminEmail, "Shop Owner");
        var client = await CreateSignedInClientAsync();

        var me = await client.GetFromJsonAsync<MeResponse>("/api/me");

        Assert.Equal((TestSettings.AdminEmail, "Shop Owner", "Admin"), (me!.Email, me.Name, me.Role));
    }

    [Fact]
    public async Task Me_WithoutAToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithIssuedToken_IsAllowed()
    {
        _factory.GoogleTokens.Accept("admin-token", "google-sub-1", TestSettings.AdminEmail);
        var client = await CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("Shoes", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithATamperedToken_ReturnsUnauthorized()
    {
        _factory.GoogleTokens.Accept("admin-token", "google-sub-1", TestSettings.AdminEmail);
        var session = await SignInAsync("admin-token");

        // Flip the last character of the signature: everything else about the token still reads
        // as a valid admin session, so only the signature check can catch it.
        var tampered = session.Token[..^1] + (session.Token[^1] == 'A' ? 'B' : 'A');
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("Shoes", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> CreateSignedInClientAsync()
    {
        var session = await SignInAsync("admin-token");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return client;
    }

    private async Task<SessionResponse> SignInAsync(string idToken)
    {
        var response = await PostSignInAsync(idToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
    }

    private Task<HttpResponseMessage> PostSignInAsync(string? idToken) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new GoogleSignInRequest(idToken));

    private async Task<User> FindUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        return await db.Users.SingleAsync(u => u.Email == email);
    }
}
