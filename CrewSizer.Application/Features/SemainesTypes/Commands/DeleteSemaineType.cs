using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.SemainesTypes.Commands;

public record DeleteSemaineTypeCommand(Guid Id) : IRequest;

public class DeleteSemaineTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<DeleteSemaineTypeCommand>
{
    public async Task Handle(DeleteSemaineTypeCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var semaine = await db.SemainesTypes.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Semaine type '{request.Id}' introuvable.");

        // Vérifier qu'aucun scénario ne référence cette semaine type dans son calendrier
        var usedInScenario = await db.Scenarios
            .AnyAsync(s => s.Calendrier.Any(c => c.SemaineTypeId == request.Id), cancellationToken);

        if (usedInScenario)
            throw new InvalidOperationException(
                $"Impossible de supprimer la semaine type '{semaine.Reference}' : elle est utilisée dans un ou plusieurs calendriers.");

        db.SemainesTypes.Remove(semaine);
        await db.SaveChangesAsync(cancellationToken);
    }
}
