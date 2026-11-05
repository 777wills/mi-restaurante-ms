using Microsoft.EntityFrameworkCore;
using MiRestaurante.Application;

namespace MiRestaurante.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext;
