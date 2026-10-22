using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Shop.Api.Catalog;
using Shop.Api.Data;
using Shop.Api.Domain;
using Shop.Api.Phone;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// The send caps and the one-answer rule are the substance of phone verification, so most of
/// these tests age the stored rows to step past a cooldown or out of a window rather than wait.
/// </summary>
public sealed class PhoneApiTests : IDisposable
{
    private const string Typed = "01712-345678";
    private const string Phone = "+8801712345678";
    private const string OtherPhone = "+8801812345678";

    private readonly ShopApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SendCode_ValidNumber_TextsASixDigitCodeToTheNormalisedNumber()
    {
        var response = await SendAsync(Typed);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SendCodeResponse>();
        Assert.Equal(new SendCodeResponse(Phone, 300, 60), body);
        var sent = Assert.Single(_factory.Sms.Sent);
        Assert.Equal(Phone, sent.Phone);
        Assert.True(OtpCodes.IsWellFormed(_factory.Sms.LastCodeFor(Phone)));
    }

    [Fact]
    public async Task SendCode_ResponseNeverContainsTheCode()
    {
        var response = await SendAsync(Typed);

        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(_factory.Sms.LastCodeFor(Phone), text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendCode_StoresAHashAndNotTheCode()
    {
        await SendAsync(Typed);

        var row = Assert.Single(await RowsAsync());
        Assert.DoesNotContain(_factory.Sms.LastCodeFor(Phone), row.CodeHash, StringComparison.Ordinal);
        Assert.Equal(64, row.CodeHash.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("02912345678")]
    public async Task SendCode_NotABangladeshiMobile_ReturnsBadRequestOnThePhoneField(string? phone)
    {
        var response = await SendAsync(phone);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("phone", problem!.Errors.Keys);
        Assert.Empty(_factory.Sms.Sent);
    }

    [Fact]
    public async Task SendCode_AgainWithinTheCooldown_Returns429WithTheWait()
    {
        await SendAsync(Typed);

        var response = await SendAsync(Typed);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var seconds = await RetryAfterSecondsAsync(response);
        Assert.InRange(seconds, 55, 60);
        Assert.Equal(TimeSpan.FromSeconds(seconds), response.Headers.RetryAfter?.Delta);
        Assert.Single(_factory.Sms.Sent);
    }

    [Fact]
    public async Task SendCode_AfterTheCooldown_SendsAgain()
    {
        await SendAsync(Typed);
        await AgeRowsAsync(TimeSpan.FromSeconds(61));

        var response = await SendAsync(Typed);

        response.EnsureSuccessStatusCode();
        Assert.Equal(2, _factory.Sms.Sent.Count);
    }

    [Fact]
    public async Task SendCode_FourthInAnHour_IsRefusedUntilTheFirstAgesOut()
    {
        for (var send = 0; send < 3; send++)
        {
            (await SendAsync(Typed)).EnsureSuccessStatusCode();
            await AgeRowsAsync(TimeSpan.FromMinutes(5));
        }

        var response = await SendAsync(Typed);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        // The first send is 15 minutes old, so the hour reopens in 45.
        Assert.InRange(await RetryAfterSecondsAsync(response), 44 * 60, 45 * 60);
    }

    [Fact]
    public async Task SendCode_EleventhInADay_IsRefused()
    {
        await SeedSendsAsync(count: 10, spacing: TimeSpan.FromHours(2), newestAge: TimeSpan.FromHours(1.5));

        var response = await SendAsync(Typed);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        // The oldest of the ten went out 19.5 hours ago, so the day reopens in 4.5 hours.
        Assert.InRange(await RetryAfterSecondsAsync(response), (int)(4.5 * 3600) - 5, (int)(4.5 * 3600));
    }

    [Fact]
    public async Task SendCode_SendsOlderThanADay_DoNotCount()
    {
        await SeedSendsAsync(count: 10, spacing: TimeSpan.FromMinutes(1), newestAge: TimeSpan.FromHours(25));

        var response = await SendAsync(Typed);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SendCode_CapsArePerPhone()
    {
        await SendAsync(Typed);

        var response = await SendAsync(OtherPhone);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SendCode_ProviderFails_Returns502AndStillCountsTheSend()
    {
        _factory.Sms.FailNextSend = true;

        var failed = await SendAsync(Typed);
        var retried = await SendAsync(Typed);

        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, retried.StatusCode);
    }

    [Fact]
    public async Task SendCode_PastThePerIpLimit_Returns429AcrossPhones()
    {
        using var factory = LimitedFactory("Otp:SendsPerIpPerHour", "2");
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/phone/otp", new SendCodeRequest("01712345678"));
        await client.PostAsJsonAsync("/api/phone/otp", new SendCodeRequest("01812345678"));
        var response = await client.PostAsJsonAsync("/api/phone/otp", new SendCodeRequest("01912345678"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(await RetryAfterSecondsAsync(response) > 0);
    }

    // Behind the host's proxy every request arrives from the proxy, so the limit has to key on the
    // address the proxy appended — and only that one, since earlier entries are the caller's to forge.
    [Fact]
    public async Task SendCode_PerIpLimit_KeysOnTheAddressTheProxyAppended()
    {
        using var factory = LimitedFactory("Otp:SendsPerIpPerHour", "1");

        var first = await SendFromAsync(factory, "203.0.113.1", "01712345678");
        var otherCaller = await SendFromAsync(factory, "203.0.113.2", "01812345678");
        var forgedPrefix = await SendFromAsync(factory, "198.51.100.9, 203.0.113.1", "01912345678");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherCaller.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, forgedPrefix.StatusCode);
    }

    [Fact]
    public async Task Verify_RightCode_ReturnsATokenForThePhoneWithNoRole()
    {
        await SendAsync(Typed);

        var response = await VerifyAsync(Typed, _factory.Sms.LastCodeFor(Phone));

        response.EnsureSuccessStatusCode();
        var verified = (await response.Content.ReadFromJsonAsync<VerifiedPhoneResponse>())!;
        Assert.Equal(Phone, verified.Phone);
        Assert.InRange(verified.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(29), DateTimeOffset.UtcNow.AddMinutes(31));
        var token = new JsonWebTokenHandler().ReadJsonWebToken(verified.Token);
        Assert.Equal(Phone, token.GetClaim(ClaimNames.Phone).Value);
        Assert.False(token.TryGetClaim(ClaimNames.Role, out _));
    }

    [Fact]
    public async Task Verify_RightCode_MarksTheCodeConsumed()
    {
        await SendAsync(Typed);

        await VerifyAsync(Typed, _factory.Sms.LastCodeFor(Phone));

        var row = Assert.Single(await RowsAsync());
        Assert.NotNull(row.ConsumedAt);
    }

    [Fact]
    public async Task Verify_WrongCode_ReturnsBadRequestOnTheCodeField()
    {
        await SendAsync(Typed);

        var response = await VerifyAsync(Typed, WrongCode());

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_NoCodeWasSent_IsRejectedTheSameWay()
    {
        var response = await VerifyAsync(Typed, "123456");

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_ExpiredCode_IsRejected()
    {
        await SendAsync(Typed);
        await AgeRowsAsync(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        var response = await VerifyAsync(Typed, _factory.Sms.LastCodeFor(Phone));

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_SameCodeTwice_SecondIsRejected()
    {
        await SendAsync(Typed);
        var code = _factory.Sms.LastCodeFor(Phone);
        (await VerifyAsync(Typed, code)).EnsureSuccessStatusCode();

        var response = await VerifyAsync(Typed, code);

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_RightCodeAfterFiveWrongGuesses_IsRejected()
    {
        await SendAsync(Typed);
        for (var guess = 0; guess < 5; guess++)
        {
            await VerifyAsync(Typed, WrongCode());
        }

        var response = await VerifyAsync(Typed, _factory.Sms.LastCodeFor(Phone));

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_RightCodeOnTheFifthGuess_IsAccepted()
    {
        await SendAsync(Typed);
        for (var guess = 0; guess < 4; guess++)
        {
            await VerifyAsync(Typed, WrongCode());
        }

        var response = await VerifyAsync(Typed, _factory.Sms.LastCodeFor(Phone));

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Verify_EarlierCodeAfterANewOneWasSent_IsRejected()
    {
        await SendAsync(Typed);
        var first = _factory.Sms.LastCodeFor(Phone);
        await AgeRowsAsync(TimeSpan.FromSeconds(61));
        await SendAsync(Typed);
        if (first == _factory.Sms.LastCodeFor(Phone))
        {
            return; // A one-in-a-million draw of the same code; nothing to tell apart.
        }

        var response = await VerifyAsync(Typed, first);

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_CodeSentToAnotherPhone_IsRejected()
    {
        await SendAsync(OtherPhone);

        var response = await VerifyAsync(Typed, _factory.Sms.LastCodeFor(OtherPhone));

        await AssertRejectedAsync(response);
    }

    [Fact]
    public async Task Verify_NumberWrittenDifferentlyFromTheSend_IsAccepted()
    {
        await SendAsync("+880 1712 345678");

        var response = await VerifyAsync("01712345678", _factory.Sms.LastCodeFor(Phone));

        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12345")]
    [InlineData("abcdef")]
    public async Task Verify_MalformedCode_ReturnsBadRequestWithoutSpendingAnAttempt(string? code)
    {
        await SendAsync(Typed);

        var response = await VerifyAsync(Typed, code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Assert.Single(await RowsAsync()).AttemptCount);
    }

    [Fact]
    public async Task Verify_PastThePerIpLimit_Returns429()
    {
        using var factory = LimitedFactory("Otp:VerifiesPerIpPerHour", "2");
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/phone/verify", new VerifyCodeRequest(Typed, "123456"));
        await client.PostAsJsonAsync("/api/phone/verify", new VerifyCodeRequest(Typed, "123456"));
        var response = await client.PostAsJsonAsync("/api/phone/verify", new VerifyCodeRequest(Typed, "123456"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    // The phone token is signed with the same key as an admin session, so the role check is the
    // only thing keeping it out of the admin endpoints. Run against the real bearer scheme.
    [Fact]
    public async Task VerifiedPhoneToken_OnAnAdminEndpoint_ReturnsForbidden()
    {
        using var factory = new ShopApiFactory(useRealAuthentication: true);
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/phone/otp", new SendCodeRequest(Typed));
        var verify = await client.PostAsJsonAsync(
            "/api/phone/verify", new VerifyCodeRequest(Typed, factory.Sms.LastCodeFor(Phone)));
        var verified = (await verify.Content.ReadFromJsonAsync<VerifiedPhoneResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verified.Token);

        var response = await client.PostAsJsonAsync("/api/admin/categories", new CategoryRequest("Shoes", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prune_RemovesRowsOlderThanADayAndKeepsTheRest()
    {
        await SeedSendsAsync(count: 3, spacing: TimeSpan.FromHours(1), newestAge: TimeSpan.FromHours(23));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var removed = await PhoneVerificationPruner.PruneAsync(db, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, removed);
        var kept = Assert.Single(await db.PhoneVerifications.ToListAsync());
        Assert.True(kept.CreatedAt > DateTime.UtcNow.AddDays(-1));
    }

    private WebApplicationFactory<Program> LimitedFactory(string setting, string value) =>
        _factory.WithWebHostBuilder(builder => builder.UseSetting(setting, value));

    private static Task<HttpResponseMessage> SendFromAsync(
        WebApplicationFactory<Program> factory,
        string forwardedFor,
        string phone)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/phone/otp")
        {
            Content = JsonContent.Create(new SendCodeRequest(phone))
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return factory.CreateClient().SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAsync(string? phone) =>
        _factory.CreateClient().PostAsJsonAsync("/api/phone/otp", new SendCodeRequest(phone));

    private Task<HttpResponseMessage> VerifyAsync(string? phone, string? code) =>
        _factory.CreateClient().PostAsJsonAsync("/api/phone/verify", new VerifyCodeRequest(phone, code));

    private string WrongCode()
    {
        var right = int.Parse(_factory.Sms.LastCodeFor(Phone));
        return ((right + 1) % 1_000_000).ToString("D6");
    }

    private static async Task AssertRejectedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(new[] { "code" }, problem!.Errors.Keys);
    }

    private static async Task<int> RetryAfterSecondsAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("retryAfterSeconds").GetInt32();
    }

    private async Task<List<PhoneVerification>> RowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        return await db.PhoneVerifications.AsNoTracking().ToListAsync();
    }

    /// <summary>Moves every stored send <paramref name="by"/> into the past, as if that long had passed.</summary>
    private async Task AgeRowsAsync(TimeSpan by)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        foreach (var row in await db.PhoneVerifications.ToListAsync())
        {
            row.CreatedAt -= by;
            row.ExpiresAt -= by;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Stores <paramref name="count"/> past sends to <see cref="Phone"/>, newest first going back.</summary>
    private async Task SeedSendsAsync(int count, TimeSpan spacing, TimeSpan newestAge)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var now = DateTime.UtcNow;
        for (var send = 0; send < count; send++)
        {
            var createdAt = now - newestAge - (spacing * send);
            db.PhoneVerifications.Add(new PhoneVerification
            {
                Phone = Phone,
                CodeHash = new string('0', 64),
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddMinutes(5)
            });
        }
        await db.SaveChangesAsync();
    }
}
