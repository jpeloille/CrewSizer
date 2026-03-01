using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueBlocs.Commands;

public record CreateBlocVolCommand(
    string Code,
    int Sequence,
    string Jour,
    string Periode,
    string DebutDP,
    string FinDP,
    string DebutFDP,
    string FinFDP,
    List<EtapeVolDto> Etapes,
    Guid TypeAvionId,
    Guid? BlocTypeId = null
) : IRequest<BlocVolDto>;

public class CreateBlocVolValidator : AbstractValidator<CreateBlocVolCommand>
{
    public CreateBlocVolValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code bloc est obligatoire.");
        RuleFor(x => x.Jour).NotEmpty();
        RuleFor(x => x.DebutDP).Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.FinDP).Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.DebutFDP).Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.FinFDP).Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.Etapes).NotEmpty().WithMessage("Au moins une étape est requise.");
        RuleFor(x => x.TypeAvionId).NotEmpty().WithMessage("Le type avion est obligatoire.");
    }
}

public class CreateBlocVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<CreateBlocVolCommand, BlocVolDto>
{
    public async Task<BlocVolDto> Handle(CreateBlocVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        // Vérifier unicité du code
        var exists = await db.BlocsVol.AnyAsync(b => b.Code == request.Code, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Un bloc avec le code '{request.Code}' existe déjà.");

        // Vérifier que les vols référencés existent
        var volIds = request.Etapes.Select(e => e.VolId).Distinct().ToList();
        var volsExistants = await db.Vols.Where(v => volIds.Contains(v.Id)).CountAsync(cancellationToken);
        if (volsExistants != volIds.Count)
            throw new KeyNotFoundException("Un ou plusieurs vols référencés n'existent pas.");

        var bloc = new BlocVol
        {
            Code = request.Code,
            Sequence = request.Sequence,
            Jour = request.Jour,
            Periode = request.Periode,
            DebutDP = request.DebutDP,
            FinDP = request.FinDP,
            DebutFDP = request.DebutFDP,
            FinFDP = request.FinFDP,
            BlocTypeId = request.BlocTypeId,
            TypeAvionId = request.TypeAvionId,
            Etapes = request.Etapes.Select(e => new EtapeVol
            {
                Position = e.Position,
                VolId = e.VolId,
                Modificateur = e.Modificateur
            }).ToList()
        };

        db.BlocsVol.Add(bloc);
        await db.SaveChangesAsync(cancellationToken);

        // Charger les vols pour les propriétés calculées
        var vols = await db.Vols.Where(v => volIds.Contains(v.Id)).ToListAsync(cancellationToken);
        bloc.Vols = bloc.Etapes.OrderBy(e => e.Position)
            .Select(e => vols.First(v => v.Id == e.VolId))
            .ToList();

        // Charger le BlocType et TypeAvion pour le DTO
        if (bloc.BlocTypeId.HasValue)
            bloc.BlocType = await db.BlocTypes.FindAsync([bloc.BlocTypeId.Value], cancellationToken);
        bloc.TypeAvion = await db.TypesAvion.FindAsync([bloc.TypeAvionId], cancellationToken);

        return bloc.ToDto();
    }
}
