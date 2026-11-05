using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiRestaurante.Domain;

namespace MiRestaurante.Infrastructure.Configurations;

internal sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    private static readonly DateTimeOffset SeedDate = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100)
            .UseCollation("case_insensitive");
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.ImageUrl).HasMaxLength(2048);
        builder.Property(d => d.Price).HasPrecision(10, 2);

        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasData(
            new Dish
            {
                Id = 1,
                Name = "Bandeja paisa",
                Description = "Fríjoles, arroz, chicharrón, carne molida, chorizo, huevo frito, tajada de maduro, aguacate y arepa.",
                Price = 38000.00m,
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/1/12/Bandepaisabog.JPG",
                IsAvailable = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new Dish
            {
                Id = 2,
                Name = "Ajiaco santafereño",
                Description = "Sopa de tres papas con pollo, mazorca y guascas, servida con crema, alcaparras y aguacate.",
                Price = 32000.00m,
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg",
                IsAvailable = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new Dish
            {
                Id = 3,
                Name = "Sancocho de hueso",
                Description = "Caldo de hueso de res con yuca, plátano, papa y mazorca, acompañado de arroz y aguacate.",
                Price = 28000.00m,
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/4/42/Sancocho-hueso.JPG",
                IsAvailable = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new Dish
            {
                Id = 4,
                Name = "Lechona",
                Description = "Cerdo relleno de arroz y arveja, horneado durante horas, servido con arepa e insulso.",
                Price = 30000.00m,
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/9/92/Plato_Lechona.jpg",
                IsAvailable = false,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new Dish
            {
                Id = 5,
                Name = "Arepa de huevo",
                Description = "Arepa de maíz frita rellena de huevo, típica de la costa Caribe.",
                Price = 8000.00m,
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/2/21/Arepa_de_huevo_por_dentro.jpg",
                IsAvailable = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            });
    }
}
