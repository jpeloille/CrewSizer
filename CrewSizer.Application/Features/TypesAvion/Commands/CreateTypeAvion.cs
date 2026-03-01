using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.TypesAvion.Commands;

public record CreateTypeAvionCommand(
    string Code,
    string Libelle,
    int NbCdb,
    int NbOpl,
    int NbCc,
    int NbPnc
) : IRequest<TypeAvionDto>;

public class CreateTypeAvionValidator : AbstractValidator<CreateTypeAvionCommand>
{
    public CreateTypeAvionValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Le code est obligatoire.");
        RuleFor(x => x.Libelle).NotEmpty().WithMessage("Le libellé est obligatoire.");
        RuleFor(x => x.NbCdb).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbOpl).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbCc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NbPnc).GreaterThanOrEqualTo(0);
    }
}

public class CreateTypeAvionHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<CreateTypeAvionCommand, TypeAvionDto>
{
    public async Task<TypeAvionDto> Handle(CreateTypeAvionCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.TypesAvion.AnyAsync(t => t.Code == request.Code, ct);
        if (exists)
            throw new InvalidOperationException(
                $"Un TypeAvion avec le code '{request.Code}' existe déjà.");

        var entity = new TypeAvion
        {
            Code = request.Code,
            Libelle = request.Libelle,
            NbCdb = request.NbCdb,
            NbOpl = request.NbOpl,
            NbCc = request.NbCc,
            NbPnc = request.NbPnc
        };

        db.TypesAvion.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.ToDto();
    }
}
