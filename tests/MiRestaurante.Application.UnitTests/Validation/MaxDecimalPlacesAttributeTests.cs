using System.Globalization;
using AwesomeAssertions;
using MiRestaurante.Application.Validation;

namespace MiRestaurante.Application.UnitTests.Validation;

public class MaxDecimalPlacesAttributeTests
{
    private static readonly MaxDecimalPlacesAttribute Attribute = new(2);

    [Theory]
    [InlineData("1000.55")]
    [InlineData("1000.550")]
    [InlineData("8000.000")]
    [InlineData("0.01")]
    [InlineData("99999999.99")]
    public void IsValid_Should_ReturnTrue_When_ValueHasAtMostTwoSignificantDecimals(string value)
    {
        var isValid = Attribute.IsValid(decimal.Parse(value, CultureInfo.InvariantCulture));

        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1000.555")]
    [InlineData("0.001")]
    [InlineData("-1.555")]
    public void IsValid_Should_ReturnFalse_When_ValueHasMoreThanTwoSignificantDecimals(string value)
    {
        var isValid = Attribute.IsValid(decimal.Parse(value, CultureInfo.InvariantCulture));

        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("texto")]
    public void IsValid_Should_ReturnTrue_When_ValueIsNotDecimal(object? value)
    {
        var isValid = Attribute.IsValid(value);

        isValid.Should().BeTrue();
    }
}
