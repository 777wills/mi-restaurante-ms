using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AwesomeAssertions;
using MiRestaurante.Application.Dishes;

namespace MiRestaurante.Application.UnitTests.Dishes;

public class SaveDishRequestValidationTests
{
    private const string NameRequired = "El nombre es obligatorio.";
    private const string NameTooLong = "El nombre no puede superar los 100 caracteres.";
    private const string DescriptionTooLong = "La descripción no puede superar los 500 caracteres.";
    private const string PriceRequired = "El precio es obligatorio.";
    private const string PriceOutOfRange = "El precio debe ser mayor que 0 y menor o igual que 99.999.999,99.";
    private const string PriceDecimals = "El precio admite como máximo 2 decimales.";
    private const string ImageUrlTooLong = "La URL de la imagen no puede superar los 2048 caracteres.";
    private const string ImageUrlInvalid = "La URL de la imagen debe ser una URL absoluta http o https.";
    private const string AvailabilityRequired = "La disponibilidad es obligatoria.";

    private static SaveDishRequest ValidRequest() => new()
    {
        Name = "Ajiaco santafereño",
        Description = "Sopa de tres papas.",
        Price = 32000m,
        ImageUrl = "https://example.com/ajiaco.jpg",
        IsAvailable = true,
    };

    private static List<(string Member, string? Message)> Validate(SaveDishRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames.Select(member => (member, r.ErrorMessage))).ToList();
    }

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Validate_Should_ReturnNoErrors_When_RequestIsValid()
    {
        var errors = Validate(ValidRequest());

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_RequireName_When_NameIsMissingOrBlank(string? name)
    {
        var errors = Validate(ValidRequest() with { Name = name });

        errors.Should().Equal(("Name", NameRequired));
    }

    [Theory]
    [InlineData(101, 0)]
    [InlineData(100, 1)]
    public void Validate_Should_RejectName_When_ReceivedValueExceedsHundredCharacters(int length, int padding)
    {
        var spaces = new string(' ', padding);

        var errors = Validate(ValidRequest() with { Name = spaces + new string('a', length) + spaces });

        errors.Should().Equal(("Name", NameTooLong));
    }

    [Fact]
    public void Validate_Should_AcceptName_When_NameHasExactlyHundredCharacters()
    {
        var errors = Validate(ValidRequest() with { Name = new string('a', 100) });

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_RejectDescription_When_ReceivedValueExceedsFiveHundredCharacters()
    {
        var errors = Validate(ValidRequest() with { Description = new string('a', 501) });

        errors.Should().Equal(("Description", DescriptionTooLong));
    }

    [Fact]
    public void Validate_Should_RequirePrice_When_PriceIsMissing()
    {
        var errors = Validate(ValidRequest() with { Price = null });

        errors.Should().Equal(("Price", PriceRequired));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("100000000")]
    public void Validate_Should_RejectPrice_When_PriceIsOutOfRange(string price)
    {
        var errors = Validate(ValidRequest() with { Price = Parse(price) });

        errors.Should().Equal(("Price", PriceOutOfRange));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("99999999.99")]
    [InlineData("1000.550")]
    [InlineData("8000.000")]
    public void Validate_Should_AcceptPrice_When_PriceIsWithinLimits(string price)
    {
        var errors = Validate(ValidRequest() with { Price = Parse(price) });

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_RejectPrice_When_PriceHasMoreThanTwoSignificantDecimals()
    {
        var errors = Validate(ValidRequest() with { Price = 1000.555m });

        errors.Should().Equal(("Price", PriceDecimals));
    }

    [Fact]
    public void Validate_Should_ReturnOneMessagePerRule_When_PriceBreaksRangeAndDecimals()
    {
        var errors = Validate(ValidRequest() with { Price = -1.555m });

        errors.Should().BeEquivalentTo([("Price", PriceOutOfRange), ("Price", PriceDecimals)]);
    }

    [Fact]
    public void Validate_Should_RejectImageUrl_When_ReceivedValueExceedsMaximumLength()
    {
        const string prefix = "https://example.com/";

        var errors = Validate(ValidRequest() with { ImageUrl = prefix + new string('a', 2049 - prefix.Length) });

        errors.Should().Equal(("ImageUrl", ImageUrlTooLong));
    }

    [Theory]
    [InlineData("ftp://x/y")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/plain,hola")]
    [InlineData("/img.jpg")]
    public void Validate_Should_RejectImageUrl_When_UrlIsNotAbsoluteHttpOrHttps(string imageUrl)
    {
        var errors = Validate(ValidRequest() with { ImageUrl = imageUrl });

        errors.Should().Equal(("ImageUrl", ImageUrlInvalid));
    }

    [Fact]
    public void Validate_Should_AcceptImageUrl_When_ValidUrlHasSurroundingSpaces()
    {
        var errors = Validate(ValidRequest() with { ImageUrl = "  https://example.com/ajiaco.jpg  " });

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_RequireAvailability_When_IsAvailableIsMissing()
    {
        var errors = Validate(ValidRequest() with { IsAvailable = null });

        errors.Should().Equal(("IsAvailable", AvailabilityRequired));
    }
}
