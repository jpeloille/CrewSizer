using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.BlocTypes.Commands;

public record UpdateBlocTypeCommand(
    Guid Id,
    string Code,
    string Libelle,
    string DebutPlage,
    string FinPlage,
    double FdpMax,
    bool HauteSaison
) : IRequest<BlocTypeDto>;

public class UpdateBlocTypeValidator : AbstractValidator<UpdateBlocTypeCommand>
{
    public UpdateBlocTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code est obligatoire.");
        RuleFor(x => x.Libelle).NotEmpty().WithMessage("Le libellé est obligatoire.");
        RuleFor(x => x.DebutPlage).Matches(@"^\d{2}:\d{2}$")
            .WithMessage("Le début de plage doit être au format HH:mm.");
        RuleFor(x => x.FinPlage).Matches(@"^\d{2}:\d{2}$")
            .WithMessage("La fin de plage doit être au format HH:mm.");
        RuleFor(x => x.FdpMax).GreaterThan(0)
            .WithMessage("La durée FDP max doit être positive.");
    }
}

public class UpdateBlocTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<UpdateBlocTypeCommand, BlocTypeDto>
{
    public async Task<BlocTypeDto> Handle(UpdateBlocTypeCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.BlocTypes.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"BlocType '{request.Id}' introuvable.");

        if (entity.Code != request.Code)
        {
            var exists = await db.BlocTypes.AnyAsync(
                b => b.Code == request.Code && b.Id != request.Id, ct);
            if (exists)
                throw new InvalidOperationException(
                    $"Un BlocType avec le code '{request.Code}' existe déjà.");
        }

        entity.Code = request.Code;
        entity.Libelle = request.Libelle;
        entity.DebutPlage = request.DebutPlage;
        entity.FinPlage = request.FinPlage;
        entity.FdpMax = request.FdpMax;
        entity.HauteSaison = request.HauteSaison;

        await db.SaveChangesAsync(ct);
        return entity.ToDto();
    }
}
