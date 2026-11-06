using TurneraJardin.Api.Dtos;
using Xunit;

namespace TurneraJardin.Tests;

/// <summary>Cases U14–U15 of the QA report.</summary>
public class ValidationTests
{
    [Theory]
    [InlineData("juan@gmail.com", true)]
    [InlineData("ana.perez@colegio.edu.ar", true)]
    [InlineData("juan@gmail", false)]
    [InlineData("juan@@gmail.com", false)]
    [InlineData("juan gmail.com", false)]
    [InlineData("juan@gmail.c", false)]
    public void Strict_email(string email, bool valid)
    {
        Assert.Equal(valid, new StrictEmailAttribute().IsValid(email));
    }

    [Theory]
    [InlineData("223 555-1234", true)]
    [InlineData("1", false)]
    [InlineData("(((1)))", false)]
    public void Argentine_phone(string phone, bool valid)
    {
        Assert.Equal(valid, new ArgentinePhoneAttribute().IsValid(phone));
    }
}
