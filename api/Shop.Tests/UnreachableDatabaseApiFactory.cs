using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shop.Tests;

/// <summary>
/// Hosts the API with the real PostgreSQL provider pointed at a closed port, to exercise the failure path.
/// </summary>
public sealed class UnreachableDatabaseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseAuthSettings().UseSetting(
            "ConnectionStrings:Default",
            "Host=127.0.0.1;Port=1;Database=unused;Username=u;Password=p;Timeout=2");
    }
}
