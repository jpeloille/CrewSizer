using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.SemainesTypes.Commands;

public record CreateSemaineTypeCommand(
    string Reference,
    string Saison,
    List<BlocPlacementDto> Placements
) : IRequest<SemaineTypeDto>;

public class CreateSemaineTypeValidator : AbstractValidator<CreateSemaineTypeCommand>
{
    public CreateSemaineTypeValidator()
    {
        RuleFor(x => x.Reference).NotEmpty().WithMessage("La référence de semaine type est obligatoire.");
        RuleFor(x => x.Placements).NotEmpty().WithMessage("Au moins un placement est requis.");
    }
}

public class CreateSemaineTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<CreateSemaineTypeCommand, SemaineTypeDto>
{
    public async Task<SemaineTypeDto> Handle(CreateSemaineTypeCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var exists = await db.SemainesTypes.AnyAsync(s => s.Reference == request.Reference, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Une semaine type avec la référence '{request.Reference}' existe déjà.");

        // Vérifier que les blocs référencés existent
        var blocIds = request.Placements.Select(p => p.BlocId).Distinct().ToList();
        var blocsExistants = await db.BlocsVol.Where(b => blocIds.Contains(b.Id)).CountAsync(cancellationToken);
        if (blocsExistants != blocIds.Count)
            throw new KeyNotFoundException("Un ou plusieurs blocs référencés n'existent pas.");

        var semaine = new SemaineType
        {
            Reference = request.Reference,
            Saison = request.Saison,
            Placements = request.Placements.Select(p => new BlocPlacement
            {
                BlocId = p.BlocId,
                Jour = p.Jour,
                Sequence = p.Sequence
            }).ToList()
        };

        db.SemainesTypes.Add(semaine);
        await db.SaveChangesAsync(cancellationToken);

        return semaine.ToDto();
    }
}
