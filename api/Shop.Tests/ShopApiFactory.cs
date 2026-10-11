using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop.Api.Data;

namespace Shop.Tests;

/// <summary>
/// Hosts the API against an in-memory database so tests need no PostgreSQL instance.
/// </summary>
public sealed class ShopApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ShopDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ShopDbContext>>();
            services.AddDbContext<ShopDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
