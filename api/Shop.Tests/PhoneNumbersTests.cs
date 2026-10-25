using Shop.Api.Phone;

namespace Shop.Tests;

/// <summary>
/// One phone has to be one key however a customer types it, or the send caps and the code lookup
/// could be dodged by writing the same number differently.
/// </summary>
public sealed class PhoneNumbersTests
{
    [Theory]
    [InlineData("01712345678")]
    [InlineData("+8801712345678")]
    [InlineData("8801712345678")]
    [InlineData(" 01712-345678 ")]
    [InlineData("+880 1712 345 678")]
    [InlineData("(017) 1234.5678")]
    public void TryNormalize_CommonWaysOfWritingANumber_AllGiveTheSameE164(string input)
    {
        var ok = PhoneNumbers.TryNormalize(input, out var e164);

        Assert.True(ok);
        Assert.Equal("+8801712345678", e164);
    }

    [Theory]
    [InlineData("01312345678", "+8801312345678")]
    [InlineData("01912345678", "+8801912345678")]
    public void TryNormalize_EveryOperatorPrefix_IsAccepted(string input, string expected)
    {
        Assert.True(PhoneNumbers.TryNormalize(input, out var e164));
        Assert.Equal(expected, e164);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0171234567")]                 // ten digits
    [InlineData("017123456789")]               // twelve digits
    [InlineData("01212345678")]                // 012 is not a mobile operator
    [InlineData("02912345678")]                // a landline
    [InlineData("+9101712345678")]             // another country
    [InlineData("0171234567a")]
    [InlineData("01712345678; DROP TABLE")]
    [InlineData("+880+8801712345678")]
    [InlineData("0 1 7 1 2 3 4 5 6 7 8 0 0 0 0 0 0 0 0 0")]  // too long to be worth parsing
    public void TryNormalize_NotABangladeshiMobile_IsRejected(string? input)
    {
        var ok = PhoneNumbers.TryNormalize(input, out var e164);

        Assert.False(ok);
        Assert.Null(e164);
    }
}
