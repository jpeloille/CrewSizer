using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.BlocTypes.Commands;

public record DeleteBlocTypeCommand(Guid Id) : IRequest;

public class DeleteBlocTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<DeleteBlocTypeCommand>
{
    public async Task Handle(DeleteBlocTypeCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.BlocTypes.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"BlocType '{request.Id}' introuvable.");

        var usedInBloc = await db.BlocsVol
            .AnyAsync(b => b.BlocTypeId == request.Id, ct);

        if (usedInBloc)
            throw new InvalidOperationException(
                $"Impossible de supprimer le BlocType '{entity.Code}' : " +
                "il est utilisé dans un ou plusieurs blocs.");

        db.BlocTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
