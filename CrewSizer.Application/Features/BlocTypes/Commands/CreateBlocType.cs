using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.BlocTypes.Commands;

public record CreateBlocTypeCommand(
    string Code,
    string Libelle,
    string DebutPlage,
    string FinPlage,
    double FdpMax,
    bool HauteSaison
) : IRequest<BlocTypeDto>;

public class CreateBlocTypeValidator : AbstractValidator<CreateBlocTypeCommand>
{
    public CreateBlocTypeValidator()
    {
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

public class CreateBlocTypeHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<CreateBlocTypeCommand, BlocTypeDto>
{
    public async Task<BlocTypeDto> Handle(CreateBlocTypeCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.BlocTypes.AnyAsync(b => b.Code == request.Code, ct);
        if (exists)
            throw new InvalidOperationException(
                $"Un BlocType avec le code '{request.Code}' existe déjà.");

        var entity = new BlocType
        {
            Code = request.Code,
            Libelle = request.Libelle,
            DebutPlage = request.DebutPlage,
            FinPlage = request.FinPlage,
            FdpMax = request.FdpMax,
            HauteSaison = request.HauteSaison
        };

        db.BlocTypes.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.ToDto();
    }
}
