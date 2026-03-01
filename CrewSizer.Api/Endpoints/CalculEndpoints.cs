using CrewSizer.Application.Features.Calcul.Commands;
using CrewSizer.Application.Features.Calcul.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class CalculEndpoints
{
    public static void MapCalculEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/calcul")
            .WithTags("Calcul")
            .RequireAuthorization();

        group.MapPost("/run", async (RunCalculRequest request, IMediator mediator, HttpContext ctx) =>
        {
            var userName = ctx.User.Identity?.Name;
            var dto = await mediator.Send(new RunCalculCommand(request.ScenarioId, userName));
            return Results.Ok(dto);
        });

        group.MapGet("/snapshots", async (Guid? scenarioId, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllSnapshotsQuery(scenarioId))));

        group.MapGet("/snapshots/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var dto = await mediator.Send(new GetSnapshotQuery(id));
            return dto is not null ? Results.Ok(dto) : Results.NotFound();
        });
    }
}

public record RunCalculRequest(Guid ScenarioId);
