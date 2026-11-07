using AwesomeAssertions;
using MiRestaurante.Application.Dishes;
using MiRestaurante.Domain;

namespace MiRestaurante.Application.UnitTests.Dishes;

public class DishMappingsTests
{
    [Fact]
    public void ToDto_Should_CopyAllPublicFields_When_DishIsMapped()
    {
        var createdAt = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 11, 2, 9, 30, 0, TimeSpan.Zero);
        var dish = new Dish
        {
            Id = 7,
            Name = "Ajiaco santafereño",
            Description = "Sopa de tres papas.",
            Price = 32000.00m,
            ImageUrl = "https://example.com/ajiaco.jpg",
            IsAvailable = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = new DateTimeOffset(2026, 11, 3, 10, 0, 0, TimeSpan.Zero),
        };

        var dto = dish.ToDto();

        dto.Should().BeEquivalentTo(new DishDto(
            7,
            "Ajiaco santafereño",
            "Sopa de tres papas.",
            32000.00m,
            "https://example.com/ajiaco.jpg",
            true,
            createdAt,
            updatedAt));
    }
}
