using CrewSizer.Application.Features.Sizing.Commands;
using CrewSizer.Infrastructure.Solver;
using MediatR;

namespace CrewSizer.Api.Endpoints;

public static class SizingEndpoints
{
    public static void MapSizingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sizing")
            .WithTags("Sizing (CP-SAT)")
            .RequireAuthorization();

        // POST /api/sizing/run → 202 Accepted + solveId (async background solve)
        group.MapPost("/run", (RunSizingRequest request, IMediator mediator, SolveProgressTracker tracker, HttpContext ctx) =>
        {
            var userName = ctx.User.Identity?.Name;
            var solveId = Guid.NewGuid().ToString("N")[..12];
            tracker.Register(solveId);

            // Lancer le solve en background (fire & forget avec scope DI)
            var scopeFactory = ctx.RequestServices.GetRequiredService<IServiceScopeFactory>();
            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                try
                {
                    var result = await scopedMediator.Send(new RunSizingCommand(
                        request.ScenarioId,
                        userName,
                        solveId));
                    tracker.Complete(solveId, result);
                }
                catch (Exception ex)
                {
                    tracker.Fail(solveId, ex.Message);
                }
            });

            return Results.Accepted($"/api/sizing/progress/{solveId}", new { solveId });
        });

        // GET /api/sizing/progress/{solveId} → progression courante
        group.MapGet("/progress/{solveId}", (string solveId, SolveProgressTracker tracker) =>
        {
            var progress = tracker.Get(solveId);
            return progress is null ? Results.NotFound() : Results.Ok(progress);
        });
    }
}

public record RunSizingRequest(Guid ScenarioId);
