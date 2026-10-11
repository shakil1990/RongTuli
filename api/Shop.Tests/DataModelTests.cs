using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Api.Data;
using Shop.Api.Domain;

namespace Shop.Tests;

public sealed class DataModelTests
{
    [Fact]
    public async Task Seed_CreatesCategoriesWithUniqueNamesAndSlugs()
    {
        using var factory = new ShopApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Database.EnsureCreatedAsync();

        var categories = await db.Categories.OrderBy(x => x.SortOrder).ToListAsync();

        Assert.NotEmpty(categories);
        Assert.Equal(categories.Count, categories.Select(x => x.Slug).Distinct().Count());
        Assert.Equal(categories.Count, categories.Select(x => x.Name).Distinct().Count());
    }

    [Theory]
    [InlineData(typeof(Category), nameof(Category.Name))]
    [InlineData(typeof(Category), nameof(Category.Slug))]
    [InlineData(typeof(Product), nameof(Product.Slug))]
    [InlineData(typeof(User), nameof(User.GoogleSub))]
    [InlineData(typeof(User), nameof(User.Email))]
    public void Model_EnforcesUniqueness(Type entityType, string propertyName)
    {
        using var factory = new ShopApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var entity = db.Model.FindEntityType(entityType);

        Assert.NotNull(entity);
        Assert.Contains(
            entity.GetIndexes(),
            x => x.IsUnique && x.Properties.Count == 1 && x.Properties[0].Name == propertyName);
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.CategoryId))]
    [InlineData(typeof(Product), nameof(Product.Price))]
    [InlineData(typeof(Product), nameof(Product.CreatedAt))]
    public void Model_IndexesProductCatalogQueryColumns(Type entityType, string propertyName)
    {
        using var factory = new ShopApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var entity = db.Model.FindEntityType(entityType);

        Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), x => x.Properties[0].Name == propertyName);
    }

    [Fact]
    public void Model_StoresEnumsAsStrings()
    {
        using var factory = new ShopApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var role = db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Role))!;
        var status = db.Model.FindEntityType(typeof(Order))!.FindProperty(nameof(Order.Status))!;

        Assert.Equal(typeof(string), role.GetProviderClrType());
        Assert.Equal(typeof(string), status.GetProviderClrType());
    }
}
