using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueBlocs.Commands;

public record DeleteBlocVolCommand(Guid Id) : IRequest;

public class DeleteBlocVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<DeleteBlocVolCommand>
{
    public async Task Handle(DeleteBlocVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var bloc = await db.BlocsVol.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Bloc '{request.Id}' introuvable.");

        // Vérifier qu'aucune semaine type ne référence ce bloc
        var usedInSemaine = await db.SemainesTypes
            .AnyAsync(s => s.Placements.Any(p => p.BlocId == request.Id), cancellationToken);

        if (usedInSemaine)
            throw new InvalidOperationException(
                $"Impossible de supprimer le bloc '{bloc.Code}' : il est utilisé dans une ou plusieurs semaines types.");

        db.BlocsVol.Remove(bloc);
        await db.SaveChangesAsync(cancellationToken);
    }
}
