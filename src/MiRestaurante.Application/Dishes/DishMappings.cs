using MiRestaurante.Domain;

namespace MiRestaurante.Application.Dishes;

public static class DishMappings
{
    public static DishDto ToDto(this Dish dish) => new(
        dish.Id,
        dish.Name,
        dish.Description,
        dish.Price,
        dish.ImageUrl,
        dish.IsAvailable,
        dish.CreatedAt,
        dish.UpdatedAt);
}
