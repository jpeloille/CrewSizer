using CrewSizer.Application.Features.Equipage.Commands;
using CrewSizer.Application.Features.Equipage.Queries;
using CrewSizer.Domain.Enums;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class EquipageEndpoints
{
    public static void MapEquipageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/equipage")
            .WithTags("Equipage")
            .RequireAuthorization();

        group.MapPost("/import", async (HttpRequest request, IMediator mediator) =>
        {
            var form = await request.ReadFormAsync();
            var pntFile = form.Files.GetFile("pnt");
            var pncFile = form.Files.GetFile("pnc");
            var checkStatusFile = form.Files.GetFile("checkStatus");
            var checkDescFile = form.Files.GetFile("checkDesc");

            var result = await mediator.Send(new ImportEquipageCommand(
                pntFile?.OpenReadStream(),
                pncFile?.OpenReadStream(),
                checkStatusFile?.OpenReadStream(),
                checkDescFile?.OpenReadStream()));

            return Results.Ok(result);
        }).DisableAntiforgery();

        group.MapGet("/membres", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetMembresQuery())));

        group.MapGet("/membres/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var dto = await mediator.Send(new GetMembreDetailQuery(id));
            return dto is not null ? Results.Ok(dto) : Results.NotFound();
        });

        group.MapGet("/kpi", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetEquipageKpiQuery())));

        group.MapGet("/alertes", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetAlertesQualificationsQuery())));

        group.MapGet("/checks", async (GroupeCheck? groupe, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetDefinitionsCheckQuery(groupe))));

        group.MapGet("/matrice", async (GroupeCheck? groupe, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetMatriceQualificationsQuery(groupe))));

        group.MapGet("/checks/{code}/membres", async (string code, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetMembresPourCheckQuery(code))));
    }
}
