using Microsoft.AspNetCore.Hosting;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// The configuration the API insists on before it will start. Both test hosts apply it, so a new
/// required setting is added in one place instead of breaking every factory.
/// </summary>
public static class TestSettings
{
    /// <summary>An address on the admin allowlist these tests run with.</summary>
    public const string AdminEmail = "admin@rongtuli.test";

    /// <summary>A second admin, listed below with the stray case and spacing a pasted value often carries.</summary>
    public const string SecondAdminEmail = "second.admin@rongtuli.test";

    // Any 32 bytes will do: no test asserts on the signature, only that the API accepts back what
    // it signed. Base64 so it exercises the same decoding path as a real deployment.
    private const string SigningKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";

    public static IWebHostBuilder UseAuthSettings(this IWebHostBuilder builder)
    {
        builder.UseSetting($"{JwtOptions.SectionName}:{nameof(JwtOptions.Key)}", SigningKey);
        builder.UseSetting($"{GoogleOptions.SectionName}:{nameof(GoogleOptions.ClientId)}", "test-client-id");
        builder.UseSetting($"{AdminOptions.SectionName}:{nameof(AdminOptions.Emails)}:0", AdminEmail);
        builder.UseSetting($"{AdminOptions.SectionName}:{nameof(AdminOptions.Emails)}:1", "  SECOND.Admin@RongTuli.Test  ");

        return builder;
    }
}
