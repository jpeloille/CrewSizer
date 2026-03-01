using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Features.Scenarios.Commands;
using CrewSizer.Application.Features.Scenarios.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class ScenarioEndpoints
{
    public static void MapScenarioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/scenarios")
            .WithTags("Scenarios")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllScenariosQuery())));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var dto = await mediator.Send(new GetScenarioByIdQuery(id));
            return dto is not null ? Results.Ok(dto) : Results.NotFound();
        });

        group.MapPost("/", async (CreateScenarioRequest request, IMediator mediator, HttpContext ctx) =>
        {
            var userName = ctx.User.Identity?.Name;
            var dto = await mediator.Send(new CreateScenarioCommand(request.Nom, request.Description, userName));
            return Results.Created($"/api/scenarios/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, ScenarioDto dto, IMediator mediator) =>
        {
            if (id != dto.Id) return Results.BadRequest("ID mismatch");
            return Results.Ok(await mediator.Send(new UpdateScenarioCommand(dto)));
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteScenarioCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/clone", async (Guid id, CloneScenarioRequest request, IMediator mediator, HttpContext ctx) =>
        {
            var userName = ctx.User.Identity?.Name;
            var dto = await mediator.Send(new CloneScenarioCommand(id, request.NouveauNom, userName));
            return Results.Created($"/api/scenarios/{dto.Id}", dto);
        });
    }
}

public record CreateScenarioRequest(string Nom, string? Description);
public record CloneScenarioRequest(string NouveauNom);
