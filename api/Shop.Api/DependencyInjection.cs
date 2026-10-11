using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;

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
}
