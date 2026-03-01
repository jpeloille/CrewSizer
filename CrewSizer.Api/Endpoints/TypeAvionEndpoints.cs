using CrewSizer.Application.Features.TypesAvion.Commands;
using CrewSizer.Application.Features.TypesAvion.Queries;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class TypeAvionEndpoints
{
    public static void MapTypeAvionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/types-avion")
            .WithTags("TypesAvion")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAllTypesAvionQuery())));

        group.MapPost("/", async (CreateTypeAvionRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new CreateTypeAvionCommand(
                request.Code, request.Libelle,
                request.NbCdb, request.NbOpl, request.NbCc, request.NbPnc));
            return Results.Created($"/api/types-avion/{dto.Id}", dto);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTypeAvionRequest request, IMediator mediator) =>
        {
            var dto = await mediator.Send(new UpdateTypeAvionCommand(
                id, request.Code, request.Libelle,
                request.NbCdb, request.NbOpl, request.NbCc, request.NbPnc));
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteTypeAvionCommand(id));
            return Results.NoContent();
        });
    }
}

public record CreateTypeAvionRequest(
    string Code, string Libelle,
    int NbCdb, int NbOpl, int NbCc, int NbPnc);

public record UpdateTypeAvionRequest(
    string Code, string Libelle,
    int NbCdb, int NbOpl, int NbCc, int NbPnc);
