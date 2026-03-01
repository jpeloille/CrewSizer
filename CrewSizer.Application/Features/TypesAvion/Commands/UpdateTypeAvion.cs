using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.TypesAvion.Commands;

public record UpdateTypeAvionCommand(
    Guid Id,
    string Code,
    string Libelle,
    int NbCdb,
    int NbOpl,
    int NbCc,
    int NbPnc
) : IRequest<TypeAvionDto>;

public class UpdateTypeAvionValidator : AbstractValidator<UpdateTypeAvionCommand>
{
    public UpdateTypeAvionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code est obligatoire.");
        RuleFor(x => x.Libelle).NotEmpty().WithMessage("Le libellé est obligatoire.");
        RuleFor(x => x.NbCdb).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbOpl).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbCc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbPnc).GreaterThanOrEqualTo(0);
    }
}

public class UpdateTypeAvionHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<UpdateTypeAvionCommand, TypeAvionDto>
{
    public async Task<TypeAvionDto> Handle(UpdateTypeAvionCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.TypesAvion.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"TypeAvion '{request.Id}' introuvable.");

        if (entity.Code != request.Code)
        {
            var exists = await db.TypesAvion.AnyAsync(
                t => t.Code == request.Code && t.Id != request.Id, ct);
            if (exists)
                throw new InvalidOperationException(
                    $"Un TypeAvion avec le code '{request.Code}' existe déjà.");
        }

        entity.Code = request.Code;
        entity.Libelle = request.Libelle;
        entity.NbCdb = request.NbCdb;
        entity.NbOpl = request.NbOpl;
        entity.NbCc = request.NbCc;
        entity.NbPnc = request.NbPnc;

        await db.SaveChangesAsync(ct);
        return entity.ToDto();
    }
}
