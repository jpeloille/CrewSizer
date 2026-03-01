using CrewSizer.Application.Features.BlocTypes.Commands;
using CrewSizer.Application.Features.BlocTypes.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class BlocTypeEndpoints
{
    public static void MapBlocTypeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bloc-types")
            .WithTags("BlocTypes")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllBlocTypesQuery())));

        group.MapPost("/", async (CreateBlocTypeRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new CreateBlocTypeCommand(
                request.Code, request.Libelle,
                request.DebutPlage, request.FinPlage,
                request.FdpMax, request.HauteSaison));
            return Results.Created($"/api/bloc-types/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateBlocTypeRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateBlocTypeCommand(
                id, request.Code, request.Libelle,
                request.DebutPlage, request.FinPlage,
                request.FdpMax, request.HauteSaison));
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteBlocTypeCommand(id));
            return Results.NoContent();
        });
    }
}

public record CreateBlocTypeRequest(
    string Code, string Libelle,
    string DebutPlage, string FinPlage,
    double FdpMax, bool HauteSaison);

public record UpdateBlocTypeRequest(
    string Code, string Libelle,
    string DebutPlage, string FinPlage,
    double FdpMax, bool HauteSaison);
