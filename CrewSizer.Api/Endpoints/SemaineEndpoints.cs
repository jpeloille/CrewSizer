using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Features.SemainesTypes.Commands;
using CrewSizer.Application.Features.SemainesTypes.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class SemaineEndpoints
{
    public static void MapSemaineEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/semaines")
            .WithTags("Semaines Types")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllSemainesTypesQuery())));

        group.MapPost("/", async (CreateSemaineRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new CreateSemaineTypeCommand(
                request.Reference, request.Saison, request.Placements));
            return Results.Created($"/api/semaines/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateSemaineRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateSemaineTypeCommand(
                id, request.Reference, request.Saison, request.Placements));
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteSemaineTypeCommand(id));
            return Results.NoContent();
        });
    }
}

public record CreateSemaineRequest(string Reference, string Saison, List<BlocPlacementDto> Placements);
public record UpdateSemaineRequest(string Reference, string Saison, List<BlocPlacementDto> Placements);
