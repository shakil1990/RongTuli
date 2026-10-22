using System.Text;
using Shop.Api.Phone;

namespace Shop.Tests;

public sealed class OtpCodesTests
{
    private const string Phone = "+8801712345678";

    private static OtpCodes CreateSut(string key = "0123456789abcdef0123456789abcdef") =>
        new(Encoding.UTF8.GetBytes(key));

    [Fact]
    public void Generate_AlwaysGivesSixDigits()
    {
        var generated = Enumerable.Range(0, 200).Select(_ => OtpCodes.Generate()).ToList();

        Assert.All(generated, code => Assert.True(OtpCodes.IsWellFormed(code), code));
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData("١٢٣٤٥٦", false)]  // Arabic-Indic digits pass char.IsDigit but are not a code
    [InlineData(null, false)]
    public void IsWellFormed_AcceptsOnlySixAsciiDigits(string? code, bool expected)
    {
        Assert.Equal(expected, OtpCodes.IsWellFormed(code));
    }

    [Fact]
    public void Hash_NeverContainsTheCode()
    {
        var hash = CreateSut().Hash(Phone, "123456");

        Assert.DoesNotContain("123456", hash, StringComparison.Ordinal);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Matches_TheCodeThatWasHashed_IsTrue()
    {
        var sut = CreateSut();

        Assert.True(sut.Matches(Phone, "123456", sut.Hash(Phone, "123456")));
    }

    [Fact]
    public void Matches_ADifferentCode_IsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.Matches(Phone, "123457", sut.Hash(Phone, "123456")));
    }

    [Fact]
    public void Matches_TheSameCodeForAnotherPhone_IsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.Matches("+8801812345678", "123456", sut.Hash(Phone, "123456")));
    }

    // A hash made under one signing key must be useless under another, so rotating Jwt__Key
    // also retires every code in flight rather than leaving them checkable.
    [Fact]
    public void Matches_AHashMadeWithAnotherKey_IsFalse()
    {
        var other = CreateSut("fedcba9876543210fedcba9876543210");

        Assert.False(CreateSut().Matches(Phone, "123456", other.Hash(Phone, "123456")));
    }
}
