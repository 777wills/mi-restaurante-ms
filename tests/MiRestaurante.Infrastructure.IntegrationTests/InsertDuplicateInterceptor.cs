using Microsoft.EntityFrameworkCore.Diagnostics;
using MiRestaurante.Domain;

namespace MiRestaurante.Infrastructure.IntegrationTests;

// Inserts an active dish with the same name from another context just before the context under test saves,
// reproducing deterministically a request that loses the race between the uniqueness check and the save.
public sealed class InsertDuplicateInterceptor(MigratedDatabaseFixture database, string name) : SaveChangesInterceptor
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await using var context = database.CreateContext();
        context.Dishes.Add(new Dish
        {
            Name = name,
            Price = 25000.00m,
            IsAvailable = true,
            CreatedAt = CreatedAt,
            UpdatedAt = CreatedAt,
        });
        await context.SaveChangesAsync(cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
