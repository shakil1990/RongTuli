using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Api.Data;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without running the host, so migrations can be generated
/// without database credentials. The connection string is a placeholder and is never used to connect.
/// </summary>
internal sealed class ShopDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=shop_design_time")
            .Options;

        return new ShopDbContext(options);
    }
}
