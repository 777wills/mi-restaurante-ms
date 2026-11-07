using System.ComponentModel.DataAnnotations;
using MiRestaurante.Application.Validation;

namespace MiRestaurante.Application.Dishes;

public sealed record SaveDishRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? Name { get; init; }

    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        MinimumIsExclusive = true,
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "El precio debe ser mayor que 0 y menor o igual que 99.999.999,99.")]
    [MaxDecimalPlaces(2, ErrorMessage = "El precio admite como máximo 2 decimales.")]
    public decimal? Price { get; init; }

    [MaxLength(2048, ErrorMessage = "La URL de la imagen no puede superar los 2048 caracteres.")]
    [HttpUrl(ErrorMessage = "La URL de la imagen debe ser una URL absoluta http o https.")]
    public string? ImageUrl { get; init; }

    [Required(ErrorMessage = "La disponibilidad es obligatoria.")]
    public bool? IsAvailable { get; init; }
}
