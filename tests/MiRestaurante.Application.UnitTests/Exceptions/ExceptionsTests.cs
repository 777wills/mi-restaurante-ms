using AwesomeAssertions;
using MiRestaurante.Application.Exceptions;

namespace MiRestaurante.Application.UnitTests.Exceptions;

public class ExceptionsTests
{
    private sealed class DerivedNotFoundException(string message) : NotFoundException(message);

    private sealed class DerivedBusinessRuleException(string message) : BusinessRuleException(message);

    [Fact]
    public void Constructor_Should_KeepMessage_When_NotFoundExceptionIsCreated()
    {
        var exception = new NotFoundException("No existe el plato 7");

        exception.Message.Should().Be("No existe el plato 7");
    }

    [Fact]
    public void Constructor_Should_KeepMessage_When_BusinessRuleExceptionIsCreated()
    {
        var exception = new BusinessRuleException("Plato no disponible");

        exception.Message.Should().Be("Plato no disponible");
    }

    [Fact]
    public void NotFoundException_Should_BeAssignableToNotFoundException_When_DerivedTypeIsUsed()
    {
        Exception exception = new DerivedNotFoundException("No existe");

        exception.Should().BeAssignableTo<NotFoundException>();
    }

    [Fact]
    public void BusinessRuleException_Should_BeAssignableToBusinessRuleException_When_DerivedTypeIsUsed()
    {
        Exception exception = new DerivedBusinessRuleException("No permitido");

        exception.Should().BeAssignableTo<BusinessRuleException>();
    }
}
