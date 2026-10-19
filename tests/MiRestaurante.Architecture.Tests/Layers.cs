using System.Reflection;

namespace MiRestaurante.Architecture.Tests;

internal static class Layers
{
    public const string Domain = "MiRestaurante.Domain";
    public const string Application = "MiRestaurante.Application";
    public const string Infrastructure = "MiRestaurante.Infrastructure";
    public const string Api = "MiRestaurante.Api";
    public const string Controllers = "MiRestaurante.Api.Controllers";

    // Assemblies are loaded by name so the tests do not depend on any specific type
    // that may be renamed or deleted as the project evolves.
    public static Assembly Load(string layer) => Assembly.Load(layer);
}
