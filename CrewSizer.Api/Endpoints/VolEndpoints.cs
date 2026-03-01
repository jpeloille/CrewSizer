using CrewSizer.Application.Features.CatalogueVols.Commands;
using CrewSizer.Application.Features.CatalogueVols.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class VolEndpoints
{
    public static void MapVolEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/vols")
            .WithTags("Vols")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllVolsQuery())));

        group.MapPost("/", async (CreateVolRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new CreateVolCommand(
                request.Numero, request.Depart, request.Arrivee,
                request.HeureDepart, request.HeureArrivee, request.MH));
            return Results.Created($"/api/vols/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateVolRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateVolCommand(
                id, request.Numero, request.Depart, request.Arrivee,
                request.HeureDepart, request.HeureArrivee, request.MH));
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteVolCommand(id));
            return Results.NoContent();
        });
    }
}

public record CreateVolRequest(string Numero, string Depart, string Arrivee, string HeureDepart, string HeureArrivee, bool MH = false);
public record UpdateVolRequest(string Numero, string Depart, string Arrivee, string HeureDepart, string HeureArrivee, bool MH = false);
