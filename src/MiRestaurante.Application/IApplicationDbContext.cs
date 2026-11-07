using Microsoft.EntityFrameworkCore;
using MiRestaurante.Domain;

namespace MiRestaurante.Application;

public interface IApplicationDbContext
{
    DbSet<Dish> Dishes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
