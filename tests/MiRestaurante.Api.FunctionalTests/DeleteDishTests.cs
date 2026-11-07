using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiRestaurante.Infrastructure;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

public class DeleteDishTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<DishResponse> CreateAsync(string name) =>
        await ReadDishAsync(await factory.CreateClient().PostAsync(BasePath, Json(ValidBody(name)), Token));

    private Task<HttpResponseMessage> DeleteAsync(int id) =>
        factory.CreateClient().DeleteAsync($"{BasePath}/{id}", Token);

    [Fact]
    public async Task Delete_Should_ReturnNoContent_When_DishIsActive()
    {
        var dish = await CreateAsync("Ajiaco");

        var response = await DeleteAsync(dish.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Should_ReturnEmptyBody_When_DishIsActive()
    {
        var dish = await CreateAsync("Ajiaco");

        var response = await DeleteAsync(dish.Id);

        (await response.Content.ReadAsStringAsync(Token)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Should_ReturnNotFound_When_DishIsDeletedTwice()
    {
        var dish = await CreateAsync("Ajiaco");
        await DeleteAsync(dish.Id);

        var response = await DeleteAsync(dish.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_ReturnNotFound_When_DishDoesNotExist()
    {
        var response = await DeleteAsync(999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_ReturnNotFoundOnGet_When_DishWasDeletedThroughApi()
    {
        var dish = await CreateAsync("Ajiaco");
        await DeleteAsync(dish.Id);

        var response = await factory.CreateClient().GetAsync($"{BasePath}/{dish.Id}", Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_ReturnNotFoundOnPut_When_DishWasDeletedThroughApi()
    {
        var dish = await CreateAsync("Ajiaco");
        await DeleteAsync(dish.Id);

        var response = await factory.CreateClient().PutAsync($"{BasePath}/{dish.Id}", Json(ValidBody("Ajiaco")), Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_HideDishFromList_When_DishWasDeletedThroughApi()
    {
        var dish = await CreateAsync("Ajiaco");
        await DeleteAsync(dish.Id);

        var names = await ReadNamesAsync(await factory.CreateClient().GetAsync(BasePath, Token));

        names.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Should_KeepRecordWithDeletedAt_When_DishIsDeleted()
    {
        var dish = await CreateAsync("Ajiaco");
        await DeleteAsync(dish.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Dishes.SingleAsync(d => d.Id == dish.Id, Token);

        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_Should_CompleteCreateReadUpdateDeleteInFourRequests_When_DishIsManaged()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsync(BasePath, Json(ValidBody("Ajiaco")), Token);
        var id = (await ReadDishAsync(created)).Id;

        var read = await client.GetAsync($"{BasePath}/{id}", Token);
        var updated = await client.PutAsync($"{BasePath}/{id}", Json(ValidBody("Sancocho")), Token);
        var deleted = await client.DeleteAsync($"{BasePath}/{id}", Token);

        new[] { created, read, updated, deleted }.Select(r => (int)r.StatusCode).Should().Equal(201, 200, 200, 204);
    }
}
