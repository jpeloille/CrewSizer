using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueVols.Commands;

public record DeleteVolCommand(Guid Id) : IRequest;

public class DeleteVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<DeleteVolCommand>
{
    public async Task Handle(DeleteVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var vol = await db.Vols.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Vol '{request.Id}' introuvable.");

        // Vérifier qu'aucun bloc ne référence ce vol
        var usedInBloc = await db.BlocsVol
            .AnyAsync(b => b.Etapes.Any(e => e.VolId == request.Id), cancellationToken);

        if (usedInBloc)
            throw new InvalidOperationException(
                $"Impossible de supprimer le vol '{vol.Numero}' : il est utilisé dans un ou plusieurs blocs.");

        db.Vols.Remove(vol);
        await db.SaveChangesAsync(cancellationToken);
    }
}
