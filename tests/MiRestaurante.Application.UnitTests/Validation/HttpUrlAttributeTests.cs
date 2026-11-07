using AwesomeAssertions;
using MiRestaurante.Application.Validation;

namespace MiRestaurante.Application.UnitTests.Validation;

public class HttpUrlAttributeTests
{
    private static readonly HttpUrlAttribute Attribute = new();

    [Theory]
    [InlineData("http://example.com/a.jpg")]
    [InlineData("https://example.com/a.jpg")]
    [InlineData("  https://example.com/a.jpg  ")]
    public void IsValid_Should_ReturnTrue_When_ValueIsAbsoluteHttpOrHttpsUrl(string value)
    {
        var isValid = Attribute.IsValid(value);

        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_Should_ReturnTrue_When_ValueIsNullOrWhiteSpace(string? value)
    {
        var isValid = Attribute.IsValid(value);

        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ftp://example.com/a.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("/images/a.jpg")]
    [InlineData("no es una url")]
    public void IsValid_Should_ReturnFalse_When_ValueIsNotAbsoluteHttpOrHttpsUrl(string value)
    {
        var isValid = Attribute.IsValid(value);

        isValid.Should().BeFalse();
    }
}
