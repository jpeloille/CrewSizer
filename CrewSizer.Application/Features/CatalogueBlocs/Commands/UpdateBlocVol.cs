using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueBlocs.Commands;

public record UpdateBlocVolCommand(
    Guid Id,
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

public class UpdateBlocVolValidator : AbstractValidator<UpdateBlocVolCommand>
{
    public UpdateBlocVolValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Etapes).NotEmpty();
        RuleFor(x => x.TypeAvionId).NotEmpty().WithMessage("Le type avion est obligatoire.");
    }
}

public class UpdateBlocVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<UpdateBlocVolCommand, BlocVolDto>
{
    public async Task<BlocVolDto> Handle(UpdateBlocVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var bloc = await db.BlocsVol.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Bloc '{request.Id}' introuvable.");

        // Vérifier unicité du code si changé
        if (bloc.Code != request.Code)
        {
            var exists = await db.BlocsVol.AnyAsync(
                b => b.Code == request.Code && b.Id != request.Id, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Un bloc avec le code '{request.Code}' existe déjà.");
        }

        bloc.Code = request.Code;
        bloc.Sequence = request.Sequence;
        bloc.Jour = request.Jour;
        bloc.Periode = request.Periode;
        bloc.DebutDP = request.DebutDP;
        bloc.FinDP = request.FinDP;
        bloc.DebutFDP = request.DebutFDP;
        bloc.FinFDP = request.FinFDP;
        bloc.BlocTypeId = request.BlocTypeId;
        bloc.TypeAvionId = request.TypeAvionId;
        bloc.Etapes = request.Etapes.Select(e => new EtapeVol
        {
            Position = e.Position,
            VolId = e.VolId,
            Modificateur = e.Modificateur
        }).ToList();

        await db.SaveChangesAsync(cancellationToken);

        // Charger les vols pour les propriétés calculées
        var volIds = bloc.Etapes.Select(e => e.VolId).Distinct().ToList();
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
