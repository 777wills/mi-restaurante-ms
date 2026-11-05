using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace MiRestaurante.Architecture.Tests;

public class ControllerConventionTests
{
    private const string RoutePrefix = "api/v1/";

    private static IEnumerable<Type> Controllers() =>
        Layers.Load(Layers.Api).GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(type));

    private static IEnumerable<MethodInfo> Actions() =>
        Controllers().SelectMany(controller => controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName && !method.IsDefined(typeof(NonActionAttribute), inherit: true)));

    private static IEnumerable<string?> Templates(MemberInfo member) =>
        member.GetCustomAttributes(inherit: true).OfType<IRouteTemplateProvider>().Select(provider => provider.Template);

    private static string WithoutAbsolutePrefix(string template) =>
        template.TrimStart('~').TrimStart('/');

    private static bool IsAbsolute(string? template) =>
        template is not null
        && (template.StartsWith('/') || template.StartsWith("~/", StringComparison.Ordinal));

    private static string Name(MethodInfo method) => $"{method.DeclaringType!.FullName}.{method.Name}";

    [Fact]
    public void Controllers_Should_HaveApiControllerAttribute_When_Compiled()
    {
        IEnumerable<string?> violations = Controllers()
            .Where(controller => !controller.IsDefined(typeof(ApiControllerAttribute), inherit: true))
            .Select(controller => controller.FullName);

        // HaveCount(0) instead of BeEmpty() so the failure message lists every offender.
        violations.Should().HaveCount(0, "every controller must be marked with [ApiController]");
    }

    [Fact]
    public void Controllers_Should_HaveRouteStartingWithApiV1_When_Compiled()
    {
        IEnumerable<string?> violations = Controllers()
            .Where(controller =>
            {
                List<string?> templates = Templates(controller).ToList();
                return templates.Count == 0
                    || templates.Any(template => template is null
                        || !template.StartsWith(RoutePrefix, StringComparison.Ordinal));
            })
            .Select(controller => controller.FullName);

        violations.Should().HaveCount(0, "every controller route must start with 'api/v1/'");
    }

    [Fact]
    public void Actions_Should_NotDeclareAbsoluteRouteOutsideApiV1_When_Compiled()
    {
        IEnumerable<string> violations = Actions()
            .Where(method => Templates(method).Any(template =>
                IsAbsolute(template)
                && !WithoutAbsolutePrefix(template!).StartsWith(RoutePrefix, StringComparison.Ordinal)))
            .Select(Name);

        violations.Should().HaveCount(0, "absolute action routes must still start with 'api/v1/'");
    }

    [Fact]
    public void Actions_Should_DeclareProducesResponseType_When_Public()
    {
        IEnumerable<string> violations = Actions()
            .Where(method => !method.IsDefined(typeof(ProducesResponseTypeAttribute), inherit: false))
            .Select(Name);

        violations.Should().HaveCount(0, "every public action must declare at least one [ProducesResponseType]");
    }
}
