using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiRestaurante.Application.Dishes;
using MiRestaurante.Application.Exceptions;
using MiRestaurante.Domain;
using Moq;

namespace MiRestaurante.Infrastructure.IntegrationTests;

public class DishServiceTests(MigratedDatabaseFixture database)
    : IClassFixture<MigratedDatabaseFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DeletedAt = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);

    // Added to the id of a deleted dish to obtain an id that does not exist.
    private const int MissingIdOffset = 1000;

    private readonly Mock<TimeProvider> _timeProvider = CreateTimeProvider();

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Mock<TimeProvider> CreateTimeProvider()
    {
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);
        return timeProvider;
    }

    private DishService CreateService(params IInterceptor[] interceptors) =>
        new(database.CreateContext(interceptors), _timeProvider.Object);

    private static SaveDishRequest Request(
        string? name,
        string? description = "Sopa de tres papas.",
        string? imageUrl = "https://example.com/ajiaco.jpg",
        decimal price = 32000.00m,
        bool isAvailable = true) => new()
        {
            Name = name,
            Description = description,
            Price = price,
            ImageUrl = imageUrl,
            IsAvailable = isAvailable,
        };

    private async Task<Dish> ReadStoredAsync(int id)
    {
        await using var context = database.CreateContext();
        return await context.Dishes.SingleAsync(d => d.Id == id, Token);
    }

    private async Task<List<string>> ReadStoredNamesAsync()
    {
        await using var context = database.CreateContext();
        return await context.Dishes.OrderBy(d => d.Id).Select(d => d.Name).ToListAsync(Token);
    }

    private async Task<List<Dish>> ReadAllStoredAsync()
    {
        await using var context = database.CreateContext();
        return await context.Dishes.OrderBy(d => d.Id).ToListAsync(Token);
    }

    private static Dish NewDish(string name, bool isAvailable = true, DateTimeOffset? deletedAt = null) => new()
    {
        Name = name,
        Description = $"Descripción de {name}",
        Price = 25000.00m,
        ImageUrl = "https://example.com/plato.jpg",
        IsAvailable = isAvailable,
        CreatedAt = CreatedAt,
        UpdatedAt = CreatedAt,
        DeletedAt = deletedAt,
    };

    private async Task AddDishesAsync(params Dish[] dishes)
    {
        await using var context = database.CreateContext();
        context.Dishes.AddRange(dishes);
        await context.SaveChangesAsync(Token);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnActiveDishesOrderedByNameIgnoringCase_When_NoFilterIsGiven()
    {
        await AddDishesAsync(
            NewDish("banana"), NewDish("Zeta"), NewDish("ñame"), NewDish("Apple"),
            NewDish("cherry"), NewDish("nube"), NewDish("árbol"));

        var dishes = await CreateService().ListAsync(null, Token);

        dishes.Select(d => d.Name).Should().Equal("Apple", "árbol", "banana", "cherry", "nube", "ñame", "Zeta");
    }

    [Theory]
    [InlineData(true, "Disponible")]
    [InlineData(false, "Agotado")]
    public async Task ListAsync_Should_ReturnOnlyDishesWithThatAvailability_When_FilterIsGiven(
        bool available, string expectedName)
    {
        await AddDishesAsync(NewDish("Disponible", isAvailable: true), NewDish("Agotado", isAvailable: false));

        var dishes = await CreateService().ListAsync(available, Token);

        dishes.Select(d => d.Name).Should().Equal(expectedName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ListAsync_Should_ExcludeDeletedDishes_When_FilterIsAnyValue(bool? available)
    {
        await AddDishesAsync(
            NewDish("Activo", isAvailable: true),
            NewDish("Agotado", isAvailable: false),
            NewDish("Eliminado", isAvailable: true, deletedAt: DeletedAt));

        var dishes = await CreateService().ListAsync(available, Token);

        dishes.Select(d => d.Name).Should().NotContain("Eliminado");
    }

    [Fact]
    public async Task ListAsync_Should_ReturnEmptyList_When_NoActiveDishesExist()
    {
        await AddDishesAsync(NewDish("Eliminado", deletedAt: DeletedAt));

        var dishes = await CreateService().ListAsync(null, Token);

        dishes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnDish_When_DishIsActive()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        var result = await CreateService().GetByIdAsync(dish.Id, Token);

        result.Should().BeEquivalentTo(new DishDto(
            dish.Id,
            "Ajiaco",
            "Descripción de Ajiaco",
            25000.00m,
            "https://example.com/plato.jpg",
            true,
            CreatedAt,
            CreatedAt));
    }

    [Fact]
    public async Task GetByIdAsync_Should_ThrowNotFoundException_When_DishDoesNotExist()
    {
        var act = () => CreateService().GetByIdAsync(999, Token);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("No existe el plato 999.");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ThrowNotFoundException_When_DishIsDeleted()
    {
        var dish = NewDish("Eliminado", deletedAt: DeletedAt);
        await AddDishesAsync(dish);

        var act = () => CreateService().GetByIdAsync(dish.Id, Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Should_SetCreatedAndUpdatedAtToCurrentTime_When_RequestIsValid()
    {
        var result = await CreateService().CreateAsync(Request("Ajiaco"), Token);

        new[] { result.CreatedAt, result.UpdatedAt }.Should().Equal(Now, Now);
    }

    [Fact]
    public async Task CreateAsync_Should_PersistDishWithoutDeletedAt_When_RequestIsValid()
    {
        var result = await CreateService().CreateAsync(Request("Ajiaco"), Token);

        var stored = await ReadStoredAsync(result.Id);

        stored.Should().BeEquivalentTo(new Dish
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            Price = result.Price,
            ImageUrl = result.ImageUrl,
            IsAvailable = result.IsAvailable,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt,
            DeletedAt = null,
        });
    }

    [Fact]
    public async Task CreateAsync_Should_TrimTextFields_When_ValuesHaveSurroundingSpaces()
    {
        var result = await CreateService().CreateAsync(
            Request("  Ajiaco  ", "  Sopa de tres papas.  ", "  https://example.com/ajiaco.jpg  "), Token);

        var stored = await ReadStoredAsync(result.Id);

        new { stored.Name, stored.Description, stored.ImageUrl }.Should().BeEquivalentTo(new
        {
            Name = "Ajiaco",
            Description = "Sopa de tres papas.",
            ImageUrl = "https://example.com/ajiaco.jpg",
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_Should_StoreNull_When_OptionalTextIsEmptyOrWhiteSpace(string? value)
    {
        var result = await CreateService().CreateAsync(Request("Ajiaco", value, value), Token);

        var stored = await ReadStoredAsync(result.Id);

        new[] { stored.Description, stored.ImageUrl }.Should().Equal(null, null);
    }

    [Theory]
    [InlineData("ajiaco", "ajiaco")]
    [InlineData(" AJIACO ", "AJIACO")]
    public async Task CreateAsync_Should_ThrowBusinessRuleException_When_ActiveDishHasSameNameIgnoringCase(
        string name, string trimmedName)
    {
        await AddDishesAsync(NewDish("Ajiaco"));

        var act = () => CreateService().CreateAsync(Request(name), Token);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage($"Ya existe un plato con el nombre «{trimmedName}».");
    }

    [Fact]
    public async Task CreateAsync_Should_NotCreateDish_When_NameIsDuplicated()
    {
        await AddDishesAsync(NewDish("Ajiaco"));

        _ = await Record.ExceptionAsync(() => CreateService().CreateAsync(Request("AJIACO"), Token));

        (await ReadStoredNamesAsync()).Should().Equal("Ajiaco");
    }

    [Fact]
    public async Task CreateAsync_Should_CreateDish_When_SameNameBelongsToDeletedDish()
    {
        await AddDishesAsync(NewDish("Ajiaco", deletedAt: DeletedAt));

        var act = () => CreateService().CreateAsync(Request("Ajiaco"), Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_Should_CreateDish_When_NameDiffersOnlyInAccents()
    {
        await AddDishesAsync(NewDish("Ajiaco"));

        var act = () => CreateService().CreateAsync(Request("Ajíaco"), Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBusinessRuleException_When_DuplicateIsInsertedConcurrently()
    {
        var service = CreateService(new InsertDuplicateInterceptor(database, "Ajiaco"));

        var act = () => service.CreateAsync(Request("Ajiaco"), Token);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_Should_RethrowDbUpdateException_When_FailureIsNotDuplicateName()
    {
        var act = () => CreateService().CreateAsync(Request(new string('a', 101)), Token);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UpdateAsync_Should_ReplaceAllFields_When_RequestIsValid()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        await CreateService().UpdateAsync(
            dish.Id,
            Request("  Sancocho  ", "  Caldo de hueso.  ", "  https://example.com/sancocho.jpg  ", 28000.50m, false),
            Token);

        var stored = await ReadStoredAsync(dish.Id);
        new { stored.Name, stored.Description, stored.Price, stored.ImageUrl, stored.IsAvailable }
            .Should().BeEquivalentTo(new
            {
                Name = "Sancocho",
                Description = "Caldo de hueso.",
                Price = 28000.50m,
                ImageUrl = "https://example.com/sancocho.jpg",
                IsAvailable = false,
            });
    }

    [Fact]
    public async Task UpdateAsync_Should_SetUpdatedAtAndKeepCreatedAt_When_RequestIsValid()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        await CreateService().UpdateAsync(dish.Id, Request("Sancocho"), Token);

        var stored = await ReadStoredAsync(dish.Id);
        new { stored.CreatedAt, stored.UpdatedAt }.Should().BeEquivalentTo(new { CreatedAt, UpdatedAt = Now });
    }

    [Fact]
    public async Task UpdateAsync_Should_SetUpdatedAtToCurrentTime_When_DataIsUnchanged()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        var result = await CreateService().UpdateAsync(
            dish.Id,
            Request(dish.Name, dish.Description, dish.ImageUrl, dish.Price, dish.IsAvailable),
            Token);

        result.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task UpdateAsync_Should_AllowRename_When_OnlyCaseOfOwnNameChanges()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        await CreateService().UpdateAsync(dish.Id, Request("AJIACO"), Token);

        (await ReadStoredAsync(dish.Id)).Name.Should().Be("AJIACO");
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowBusinessRuleException_When_OtherActiveDishHasSameNameIgnoringCase()
    {
        var sancocho = NewDish("Sancocho");
        await AddDishesAsync(NewDish("Ajiaco"), sancocho);

        var act = () => CreateService().UpdateAsync(sancocho.Id, Request("ajiaco"), Token);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Ya existe un plato con el nombre «ajiaco».");
    }

    [Fact]
    public async Task UpdateAsync_Should_LeaveBothDishesUnchanged_When_NameIsDuplicated()
    {
        var ajiaco = NewDish("Ajiaco");
        var sancocho = NewDish("Sancocho");
        await AddDishesAsync(ajiaco, sancocho);

        _ = await Record.ExceptionAsync(() => CreateService().UpdateAsync(sancocho.Id, Request("AJIACO"), Token));

        (await ReadAllStoredAsync()).Should().BeEquivalentTo([ajiaco, sancocho]);
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task UpdateAsync_Should_ThrowNotFoundException_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await AddDishesAsync(NewDish("Ajiaco"), deleted);

        var act = () => CreateService().UpdateAsync(deleted.Id + offset, Request("Ajiaco"), Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowBusinessRuleException_When_DuplicateIsInsertedConcurrently()
    {
        var dish = NewDish("Sancocho");
        await AddDishesAsync(dish);
        var service = CreateService(new InsertDuplicateInterceptor(database, "Ajiaco"));

        var act = () => service.UpdateAsync(dish.Id, Request("Ajiaco"), Token);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_Should_KeepLastWrite_When_TwoUpdatesAreAppliedInSequence()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);
        var first = CreateService();
        var second = CreateService();

        await first.UpdateAsync(dish.Id, Request("Primero"), Token);
        await second.UpdateAsync(dish.Id, Request("Segundo"), Token);

        (await ReadStoredAsync(dish.Id)).Name.Should().Be("Segundo");
    }

    [Fact]
    public async Task DeleteAsync_Should_SetDeletedAtToCurrentTime_When_DishIsActive()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        await CreateService().DeleteAsync(dish.Id, Token);

        (await ReadStoredAsync(dish.Id)).DeletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task DeleteAsync_Should_KeepRecordAndOtherFields_When_DishIsDeleted()
    {
        var dish = NewDish("Ajiaco");
        await AddDishesAsync(dish);

        await CreateService().DeleteAsync(dish.Id, Token);

        (await ReadStoredAsync(dish.Id)).Should().BeEquivalentTo(dish, options => options.Excluding(d => d.DeletedAt));
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task DeleteAsync_Should_ThrowNotFoundException_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await AddDishesAsync(deleted);

        var act = () => CreateService().DeleteAsync(deleted.Id + offset, Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
