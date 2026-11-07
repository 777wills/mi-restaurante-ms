namespace MiRestaurante.Application.Dishes;

public interface IDishService
{
    Task<IReadOnlyList<DishDto>> ListAsync(bool? available, CancellationToken cancellationToken);

    Task<DishDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DishDto> CreateAsync(SaveDishRequest request, CancellationToken cancellationToken);

    Task<DishDto> UpdateAsync(int id, SaveDishRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
