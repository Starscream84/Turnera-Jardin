using TurneraJardin.Api.Services;
using Xunit;

namespace TurneraJardin.Tests;

/// <summary>Cases U08–U13 and F01–F05 of the QA report (2026-11-05).</summary>
public class PhoneNumberTests
{
    [Theory]
    [InlineData("2235551234", "5492235551234")]
    [InlineData("+54 9 223 555-1234", "5492235551234")]
    [InlineData("+54 223 555 1234", "5492235551234")]
    [InlineData("5492235551234", "5492235551234")]
    [InlineData("00 54 9 223 555 1234", "5492235551234")]
    [InlineData("0223 555-1234", "5492235551234")]
    [InlineData("(223) 555-1234", "5492235551234")]
    [InlineData("0223 15 555-1234", "5492235551234")]
    [InlineData("223 15 555 1234", "5492235551234")]
    [InlineData("011 15 4444-5555", "5491144445555")]
    [InlineData("11 4444 5555", "5491144445555")]
    [InlineData("2291 15 41-2345", "5492291412345")]
    [InlineData("+1 415 555 2671", "14155552671")]
    public void Normalizes_valid_numbers(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Normalize(input));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("(((1)))")]
    [InlineData("--------")]
    [InlineData("        ")]
    [InlineData("")]
    [InlineData("555-1234")]
    [InlineData("223 555 12345678")]
    [InlineData("٢٢٣٥٥٥١٢٣٤")]
    [InlineData("223-555-ABCD")]
    public void Rejects_invalid_numbers(string input)
    {
        Assert.Null(PhoneNumber.Normalize(input));
    }

    [Theory]
    [InlineData("+54 9 223 555-1234", "2235551234")]
    [InlineData("0223 15 555-1234", "5492235551234")]
    [InlineData("223-555-1234", "223 555 1234")]
    public void Same_number_in_different_formats_matches(string entered, string stored)
    {
        Assert.True(PhoneNumber.SameNumber(entered, stored));
    }

    [Fact]
    public void Different_numbers_do_not_match()
    {
        Assert.False(PhoneNumber.SameNumber("2235551234", "2235551235"));
        Assert.False(PhoneNumber.SameNumber("", ""));
    }
}
