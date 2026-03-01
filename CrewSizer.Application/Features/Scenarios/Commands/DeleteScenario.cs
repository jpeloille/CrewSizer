using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Commands;

public record DeleteScenarioCommand(Guid Id) : IRequest;

public class DeleteScenarioHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<DeleteScenarioCommand>
{
    public async Task Handle(DeleteScenarioCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var scenario = await db.Scenarios.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario '{request.Id}' introuvable.");

        // Supprimer les snapshots associés
        var snapshots = await db.Snapshots
            .Where(s => s.ScenarioId == request.Id)
            .ToListAsync(cancellationToken);
        db.Snapshots.RemoveRange(snapshots);

        db.Scenarios.Remove(scenario);
        await db.SaveChangesAsync(cancellationToken);
    }
}
