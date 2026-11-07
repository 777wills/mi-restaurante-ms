namespace MiRestaurante.Application.Dishes;

public sealed record DishDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
