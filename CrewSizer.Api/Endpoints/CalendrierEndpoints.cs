using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Features.Calendrier.Commands;
using CrewSizer.Application.Features.Calendrier.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class CalendrierEndpoints
{
    public static void MapCalendrierEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/calendrier")
            .WithTags("Calendrier")
            .RequireAuthorization();

        group.MapGet("/{scenarioId:guid}", async (Guid scenarioId, IMediator mediator) =>
        {
            var dto = await mediator.Send(new GetCalendrierQuery(scenarioId));
            return dto is not null ? Results.Ok(dto) : Results.NotFound();
        });

        group.MapPut("/{scenarioId:guid}", async (Guid scenarioId, UpdateCalendrierRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateCalendrierCommand(scenarioId, request.Affectations));
            return Results.Ok(dto);
        });
    }
}

public record UpdateCalendrierRequest(List<AffectationSemaineDto> Affectations);
