using Microsoft.AspNetCore.Mvc;
using MiRestaurante.Application.Dishes;

namespace MiRestaurante.Api.Controllers;

[ApiController]
[Route("api/v1/dishes")]
public sealed class DishesController(IDishService dishService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DishDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DishDto>>> List(
        [FromQuery] bool? available, CancellationToken cancellationToken) =>
        Ok(await dishService.ListAsync(available, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<DishDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await dishService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<DishDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DishDto>> Create(SaveDishRequest request, CancellationToken cancellationToken)
    {
        var dish = await dishService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<DishDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DishDto>> Update(
        int id, SaveDishRequest request, CancellationToken cancellationToken) =>
        Ok(await dishService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await dishService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
