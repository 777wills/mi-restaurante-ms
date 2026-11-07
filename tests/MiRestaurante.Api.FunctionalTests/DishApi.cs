using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MiRestaurante.Domain;
using MiRestaurante.Infrastructure;

namespace MiRestaurante.Api.FunctionalTests;

internal sealed record DishResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal static class DishApi
{
    public const string BasePath = "/api/v1/dishes";

    public static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset DeletedAt = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    public static CancellationToken Token => TestContext.Current.CancellationToken;

    public static Dish NewDish(
        string name,
        bool isAvailable = true,
        DateTimeOffset? deletedAt = null,
        string? description = "Descripción del plato",
        string? imageUrl = "https://example.com/plato.jpg") => new()
        {
            Name = name,
            Description = description,
            Price = 25000.00m,
            ImageUrl = imageUrl,
            IsAvailable = isAvailable,
            CreatedAt = CreatedAt,
            UpdatedAt = CreatedAt,
            DeletedAt = deletedAt,
        };

    public static async Task InsertAsync(ApiFactory factory, params Dish[] dishes)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Dishes.AddRange(dishes);
        await db.SaveChangesAsync(Token);
    }

    public static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    public static string ValidBody(string name = "Ajiaco", bool isAvailable = true) =>
        JsonSerializer.Serialize(new
        {
            name,
            description = "Sopa de tres papas.",
            price = 32000,
            imageUrl = "https://example.com/ajiaco.jpg",
            isAvailable,
        });

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    public static async Task<DishResponse> ReadDishAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<DishResponse>(Token))!;

    public static async Task<IReadOnlyList<string>> ReadNamesAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<List<DishResponse>>(Token))!.Select(d => d.Name).ToList();
}
