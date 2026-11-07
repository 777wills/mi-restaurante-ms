using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MiRestaurante.Application.Dishes;
using MiRestaurante.Domain;
using Moq;

namespace MiRestaurante.Infrastructure.IntegrationTests;

public class DishSeedTests(PostgreSqlFixture database)
{
    private static readonly DateTimeOffset SeedDate = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);

    private async Task<ApplicationDbContext> CreateMigratedContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(await database.CreateDatabaseAsync())
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        return context;
    }

    private static Dish SeedDish(
        int id, string name, string description, decimal price, bool isAvailable, string imageUrl) => new()
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            IsAvailable = isAvailable,
            ImageUrl = imageUrl,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate,
            DeletedAt = null,
        };

    [Fact]
    public async Task MigrateAsync_Should_CreateSeedDishes_When_DatabaseIsNew()
    {
        await using var db = await CreateMigratedContextAsync();
        Dish[] expected =
        [
            SeedDish(
                1,
                "Bandeja paisa",
                "Fríjoles, arroz, chicharrón, carne molida, chorizo, huevo frito, tajada de maduro, aguacate y arepa.",
                38000.00m,
                true,
                "https://upload.wikimedia.org/wikipedia/commons/1/12/Bandepaisabog.JPG"),
            SeedDish(
                2,
                "Ajiaco santafereño",
                "Sopa de tres papas con pollo, mazorca y guascas, servida con crema, alcaparras y aguacate.",
                32000.00m,
                true,
                "https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg"),
            SeedDish(
                3,
                "Sancocho de hueso",
                "Caldo de hueso de res con yuca, plátano, papa y mazorca, acompañado de arroz y aguacate.",
                28000.00m,
                true,
                "https://upload.wikimedia.org/wikipedia/commons/4/42/Sancocho-hueso.JPG"),
            SeedDish(
                4,
                "Lechona",
                "Cerdo relleno de arroz y arveja, horneado durante horas, servido con arepa e insulso.",
                30000.00m,
                false,
                "https://upload.wikimedia.org/wikipedia/commons/9/92/Plato_Lechona.jpg"),
            SeedDish(
                5,
                "Arepa de huevo",
                "Arepa de maíz frita rellena de huevo, típica de la costa Caribe.",
                8000.00m,
                true,
                "https://upload.wikimedia.org/wikipedia/commons/2/21/Arepa_de_huevo_por_dentro.jpg"),
        ];

        var dishes = await db.Dishes.OrderBy(d => d.Id).ToListAsync(TestContext.Current.CancellationToken);

        dishes.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task CreateAsync_Should_AssignIdAfterSeed_When_DatabaseIsFreshlyMigrated()
    {
        await using var db = await CreateMigratedContextAsync();
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);
        var service = new DishService(db, timeProvider.Object);

        var dish = await service.CreateAsync(
            new SaveDishRequest { Name = "Changua", Price = 12000m, IsAvailable = true },
            TestContext.Current.CancellationToken);

        dish.Id.Should().Be(6);
    }
}
