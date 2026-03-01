using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.SemainesTypes.Commands;

public record UpdateSemaineTypeCommand(
    Guid Id,
    string Reference,
    string Saison,
    List<BlocPlacementDto> Placements
) : IRequest<SemaineTypeDto>;

public class UpdateSemaineTypeValidator : AbstractValidator<UpdateSemaineTypeCommand>
{
    public UpdateSemaineTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty();
        RuleFor(x => x.Placements).NotEmpty();
    }
}

public class UpdateSemaineTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<UpdateSemaineTypeCommand, SemaineTypeDto>
{
    public async Task<SemaineTypeDto> Handle(UpdateSemaineTypeCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var semaine = await db.SemainesTypes.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Semaine type '{request.Id}' introuvable.");

        if (semaine.Reference != request.Reference)
        {
            var exists = await db.SemainesTypes.AnyAsync(
                s => s.Reference == request.Reference && s.Id != request.Id, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Une semaine type avec la référence '{request.Reference}' existe déjà.");
        }

        // Vérifier que les blocs référencés existent
        var blocIds = request.Placements.Select(p => p.BlocId).Distinct().ToList();
        var blocsExistants = await db.BlocsVol.Where(b => blocIds.Contains(b.Id)).CountAsync(cancellationToken);
        if (blocsExistants != blocIds.Count)
            throw new KeyNotFoundException("Un ou plusieurs blocs référencés n'existent pas.");

        semaine.Reference = request.Reference;
        semaine.Saison = request.Saison;

        // Muter la collection en place pour que EF Core détecte le changement JSONB
        semaine.Placements.Clear();
        foreach (var p in request.Placements)
        {
            semaine.Placements.Add(new BlocPlacement
            {
                BlocId = p.BlocId,
                Jour = p.Jour,
                Sequence = p.Sequence
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return semaine.ToDto();
    }
}
