using Microsoft.EntityFrameworkCore;
using MiRestaurante.Application.Exceptions;
using MiRestaurante.Domain;

namespace MiRestaurante.Application.Dishes;

public sealed class DishService(IApplicationDbContext db, TimeProvider timeProvider) : IDishService
{
    private IQueryable<Dish> ActiveDishes => db.Dishes.Where(d => d.DeletedAt == null);

    public async Task<IReadOnlyList<DishDto>> ListAsync(bool? available, CancellationToken cancellationToken)
    {
        var query = ActiveDishes;
        if (available is not null)
        {
            query = query.Where(d => d.IsAvailable == available.Value);
        }

        var dishes = await query
            .OrderBy(d => d.Name)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);
        return dishes.Select(d => d.ToDto()).ToList();
    }

    public async Task<DishDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        (await FindActiveAsync(id, cancellationToken)).ToDto();

    public async Task<DishDto> CreateAsync(SaveDishRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var dish = new Dish { CreatedAt = now };
        Apply(dish, request, now);
        db.Dishes.Add(dish);

        await SaveAsync(dish, excludedId: null, cancellationToken);
        return dish.ToDto();
    }

    // Without optimistic concurrency control: the last write wins.
    public async Task<DishDto> UpdateAsync(int id, SaveDishRequest request, CancellationToken cancellationToken)
    {
        var dish = await FindActiveAsync(id, cancellationToken);
        Apply(dish, request, timeProvider.GetUtcNow());

        await SaveAsync(dish, excludedId: dish.Id, cancellationToken);
        return dish.ToDto();
    }

    // Soft delete: the record is kept and cannot be restored.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var dish = await FindActiveAsync(id, cancellationToken);
        dish.DeletedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dish> FindActiveAsync(int id, CancellationToken cancellationToken) =>
        await ActiveDishes.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
        ?? throw new NotFoundException($"No existe el plato {id}.");

    // The request has already been validated by its DataAnnotations, so the required values are present.
    private static void Apply(Dish dish, SaveDishRequest request, DateTimeOffset now)
    {
        dish.Name = request.Name!.Trim();
        dish.Description = TrimToNull(request.Description);
        dish.Price = request.Price!.Value;
        dish.ImageUrl = TrimToNull(request.ImageUrl);
        dish.IsAvailable = request.IsAvailable!.Value;
        dish.UpdatedAt = now;
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The column collation makes the comparison case-insensitive while keeping accents significant.
    private Task<bool> NameInUseAsync(string name, int? excludedId, CancellationToken cancellationToken) =>
        ActiveDishes.AnyAsync(d => d.Name == name && (excludedId == null || d.Id != excludedId), cancellationToken);

    private async Task SaveAsync(Dish dish, int? excludedId, CancellationToken cancellationToken)
    {
        if (await NameInUseAsync(dish.Name, excludedId, cancellationToken))
        {
            throw NameInUse(dish.Name);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request may have taken the name between the check and the save; the unique index rejects it.
            if (await NameInUseAsync(dish.Name, excludedId, cancellationToken))
            {
                throw NameInUse(dish.Name);
            }

            throw;
        }
    }

    private static BusinessRuleException NameInUse(string name) =>
        new($"Ya existe un plato con el nombre «{name}».");
}
