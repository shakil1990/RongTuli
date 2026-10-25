namespace Shop.Api.Phone;

public sealed record SendCodeRequest(string? Phone);

/// <summary>
/// A code is on its way. <see cref="Phone"/> is the number as the API stored it, so the web app
/// can show the customer exactly where the code went.
/// </summary>
public sealed record SendCodeResponse(string Phone, int ExpiresInSeconds, int ResendAfterSeconds);

public sealed record VerifyCodeRequest(string? Phone, string? Code);

/// <summary>Proof the customer holds <see cref="Phone"/>; checkout sends the token as a bearer header.</summary>
public sealed record VerifiedPhoneResponse(string Token, DateTimeOffset ExpiresAt, string Phone);
