using Shop.Api.Security;

namespace Shop.Api.Phone;

public static class PhoneVerificationSetup
{
    public static IServiceCollection AddPhoneVerification(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<PhoneVerificationService>();
        services.AddHostedService<PhoneVerificationPruner>();

        // The signing key was already checked by AddAdminAuthorization; read here only to derive
        // the separate key the code hashes use.
        var signingKey = (configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions())
            .CreateSigningKey();
        services.AddSingleton(new OtpCodes(signingKey.Key));

        var provider = configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>()?.Provider?.Trim();
        if (string.IsNullOrEmpty(provider) && environment.IsDevelopment())
        {
            provider = SmsOptions.LogProvider;
        }

        if (string.Equals(provider, SmsOptions.LogProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
        }
        else
        {
            services.AddSingleton<ISmsSender, UnconfiguredSmsSender>();
        }

        // The per-IP limits on these endpoints are registered with the rest in RateLimits.
        return services;
    }
}
