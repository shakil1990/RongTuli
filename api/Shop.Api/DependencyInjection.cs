using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Images;

namespace Shop.Api;

public static class DependencyInjection
{
    private const string ConnectionName = "Default";

    public static IServiceCollection AddShopData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' is not set. Set ConnectionStrings__{ConnectionName} " +
                $"(or 'dotnet user-secrets set \"ConnectionStrings:{ConnectionName}\" ...' locally).");

        services.AddDbContext<ShopDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHealthChecks().AddDbContextCheck<ShopDbContext>("database");

        return services;
    }

    public static IServiceCollection AddImageStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));

        // The credentials are checked when the store is first resolved rather than at startup, so
        // the rest of the API — and CI, which fakes this interface — runs without a Cloudinary account.
        services.AddHttpClient<IImageStore, CloudinaryImageStore>(client =>
            {
                client.BaseAddress = new Uri("https://api.cloudinary.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
