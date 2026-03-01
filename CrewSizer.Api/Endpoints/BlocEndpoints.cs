using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Features.CatalogueBlocs.Commands;
using CrewSizer.Application.Features.CatalogueBlocs.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class BlocEndpoints
{
    public static void MapBlocEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/blocs")
            .WithTags("Blocs")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllBlocsVolQuery())));

        group.MapPost("/", async (CreateBlocRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new CreateBlocVolCommand(
                request.Code, request.Sequence, request.Jour, request.Periode,
                request.DebutDP, request.FinDP, request.DebutFDP, request.FinFDP,
                request.Etapes, request.TypeAvionId, request.BlocTypeId));
            return Results.Created($"/api/blocs/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateBlocRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateBlocVolCommand(
                id, request.Code, request.Sequence, request.Jour, request.Periode,
                request.DebutDP, request.FinDP, request.DebutFDP, request.FinFDP,
                request.Etapes, request.TypeAvionId, request.BlocTypeId));
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteBlocVolCommand(id));
            return Results.NoContent();
        });
    }
}

public record CreateBlocRequest(
    string Code, int Sequence, string Jour, string Periode,
    string DebutDP, string FinDP, string DebutFDP, string FinFDP,
    List<EtapeVolDto> Etapes, Guid TypeAvionId, Guid? BlocTypeId = null);

public record UpdateBlocRequest(
    string Code, int Sequence, string Jour, string Periode,
    string DebutDP, string FinDP, string DebutFDP, string FinFDP,
    List<EtapeVolDto> Etapes, Guid TypeAvionId, Guid? BlocTypeId = null);
