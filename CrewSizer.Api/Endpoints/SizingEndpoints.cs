using CrewSizer.Application.Features.Sizing.Commands;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class SizingEndpoints
{
    public static void MapSizingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sizing")
            .WithTags("Sizing (CP-SAT)")
            .RequireAuthorization();

        group.MapPost("/run", async (RunSizingRequest request, IMediator mediator, HttpContext ctx) =>
        {
            var userName = ctx.User.Identity?.Name;
            var result = await mediator.Send(new RunSizingCommand(
                request.ScenarioId,
                userName));
            return Results.Ok(result);
        });
    }
}

public record RunSizingRequest(Guid ScenarioId);
