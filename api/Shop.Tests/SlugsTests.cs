using Shop.Api.Catalog;
using Shop.Api.Security;

namespace Shop.Tests;

/// <summary>
/// The in-memory test database ignores column lengths, so a slug too long for its column only
/// fails on PostgreSQL. These pin the length rule where the slug is made.
/// </summary>
public sealed class SlugsTests
{
    [Fact]
    public void From_JoinsAsciiWordsWithHyphens()
    {
        Assert.Equal("red-tee", Slugs.From("  Red Tee! ", Slugs.MaxProductSlugLength));
    }

    [Fact]
    public void From_NoAsciiLetters_FallsBackToItem()
    {
        Assert.Equal("item", Slugs.From("লাল শার্ট", Slugs.MaxProductSlugLength));
    }

    [Fact]
    public void From_LongerThanTheColumn_IsCutWithoutATrailingHyphen()
    {
        // Cut at 11 characters this is "blue-shirt-"; the hyphen must not be left on the end.
        var slug = Slugs.From("Blue Shirt Large", 11);

        Assert.Equal("blue-shirt", slug);
    }

    [Fact]
    public async Task UniqueAsync_NameAtTheLengthLimitThatCollides_StillFitsTheColumn()
    {
        var name = new string('a', Slugs.MaxProductSlugLength);
        var taken = new HashSet<string> { name };

        var slug = await Slugs.UniqueAsync(name, Slugs.MaxProductSlugLength, s => Task.FromResult(taken.Contains(s)));

        Assert.Equal(Slugs.MaxProductSlugLength, slug.Length);
        Assert.EndsWith("-2", slug);
    }

    [Fact]
    public async Task UniqueAsync_ManyCollisions_KeepsEverySlugWithinTheColumn()
    {
        var name = new string('b', Slugs.MaxCategorySlugLength);
        var taken = new HashSet<string>();

        for (var i = 0; i < 12; i++)
        {
            var slug = await Slugs.UniqueAsync(name, Slugs.MaxCategorySlugLength, s => Task.FromResult(taken.Contains(s)));
            Assert.True(slug.Length <= Slugs.MaxCategorySlugLength, $"'{slug}' is {slug.Length} characters.");
            Assert.True(taken.Add(slug));
        }
    }

    [Theory]
    [InlineData("Rahim Uddin", true)]
    [InlineData("রহিম উদ্দিন", true)]
    [InlineData("Rahim\nUddin", false)]
    [InlineData("Rahim\tUddin", false)]
    [InlineData("Rahim\u0000", false)]
    public void IsSingleLine_RefusesAnyControlCharacter(string value, bool expected)
    {
        Assert.Equal(expected, TextInput.IsSingleLine(value));
    }

    [Theory]
    [InlineData("House 12\r\nRoad 5\tDhanmondi", true)]
    [InlineData("House 12\u001b[2J", false)]
    [InlineData("House\u0007 12", false)]
    public void IsMultiLine_AllowsLineBreaksAndTabsOnly(string value, bool expected)
    {
        Assert.Equal(expected, TextInput.IsMultiLine(value));
    }
}
