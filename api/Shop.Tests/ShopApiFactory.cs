using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Images;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// Hosts the API against an in-memory database so tests need no PostgreSQL instance.
/// Requests that carry the <see cref="AdminHeader"/> header are authenticated as an admin.
/// </summary>
public sealed class ShopApiFactory : WebApplicationFactory<Program>
{
    public const string AdminHeader = "X-Test-Admin";

    private const string TestScheme = "Test";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    /// <summary>The image host this instance runs against, so a test can inspect or break it.</summary>
    public FakeImageStore ImageStore { get; } = new();

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(AdminHeader, "true");
        return client;
    }

    // EnsureCreated applies the model seed data (the four categories), which an in-memory database does not get otherwise.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ShopDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ShopDbContext>>();
            services.AddDbContext<ShopDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IImageStore>();
            services.AddSingleton<IImageStore>(ImageStore);

            services.AddAuthentication(TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestScheme, null);
        });
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(AdminHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, AdminAuthorization.AdminRole)], TestScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), TestScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
