using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueVols.Commands;

public record UpdateVolCommand(
    Guid Id,
    string Numero,
    string Depart,
    string Arrivee,
    string HeureDepart,
    string HeureArrivee,
    bool MH = false
) : IRequest<VolDto>;

public class UpdateVolValidator : AbstractValidator<UpdateVolCommand>
{
    public UpdateVolValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty().WithMessage("Le numéro de vol est obligatoire.");
        RuleFor(x => x.Depart).NotEmpty();
        RuleFor(x => x.Arrivee).NotEmpty();
        RuleFor(x => x.HeureDepart).NotEmpty().Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.HeureArrivee).NotEmpty().Matches(@"^\d{2}:\d{2}$");
    }
}

public class UpdateVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<UpdateVolCommand, VolDto>
{
    public async Task<VolDto> Handle(UpdateVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var vol = await db.Vols.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Vol '{request.Id}' introuvable.");

        vol.Numero = request.Numero;
        vol.Depart = request.Depart;
        vol.Arrivee = request.Arrivee;
        vol.HeureDepart = request.HeureDepart;
        vol.HeureArrivee = request.HeureArrivee;
        vol.MH = request.MH;

        await db.SaveChangesAsync(cancellationToken);

        return vol.ToDto();
    }
}
