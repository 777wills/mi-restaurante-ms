using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiRestaurante.Application.Exceptions;

namespace MiRestaurante.Api.FunctionalTests.TestControllers;

[ApiController]
[Route("api/v1/test-errors")]
public class ErrorsController : ControllerBase
{
    public sealed class DerivedNotFoundException(string message) : NotFoundException(message);

    public sealed class DerivedBusinessRuleException(string message) : BusinessRuleException(message);

    public class BodyDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? Name { get; set; }

        public decimal Price { get; set; }
    }

    [HttpGet("not-found")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ThrowNotFound() => throw new NotFoundException("No existe el plato 7");

    [HttpGet("not-found-empty")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ThrowNotFoundEmpty() => throw new NotFoundException("");

    [HttpGet("business-rule")]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult ThrowBusinessRule() => throw new BusinessRuleException("Plato no disponible");

    [HttpGet("derived-not-found")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ThrowDerivedNotFound() => throw new DerivedNotFoundException("No existe la mesa 3");

    [HttpGet("derived-business-rule")]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult ThrowDerivedBusinessRule() => throw new DerivedBusinessRuleException("Mesa ocupada");

    [HttpGet("bad-request-413")]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public IActionResult ThrowBadHttpRequest() =>
        throw new BadHttpRequestException("Cuerpo demasiado grande", StatusCodes.Status413PayloadTooLarge);

    [HttpGet("unexpected")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException("secreto-interno");

    [HttpPost("body")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostBody([FromBody] BodyDto dto) => NoContent();
}
