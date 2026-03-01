using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.TypesAvion.Commands;

public record DeleteTypeAvionCommand(Guid Id) : IRequest;

public class DeleteTypeAvionHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<DeleteTypeAvionCommand>
{
    public async Task Handle(DeleteTypeAvionCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.TypesAvion.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"TypeAvion '{request.Id}' introuvable.");

        var usedInBloc = await db.BlocsVol
            .AnyAsync(b => b.TypeAvionId == request.Id, ct);

        if (usedInBloc)
            throw new InvalidOperationException(
                $"Impossible de supprimer le TypeAvion '{entity.Code}' : " +
                "il est utilisé dans un ou plusieurs blocs.");

        db.TypesAvion.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
