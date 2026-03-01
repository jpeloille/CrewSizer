using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueVols.Commands;

public record CreateVolCommand(
    string Numero,
    string Depart,
    string Arrivee,
    string HeureDepart,
    string HeureArrivee,
    bool MH = false
) : IRequest<VolDto>;

public class CreateVolValidator : AbstractValidator<CreateVolCommand>
{
    public CreateVolValidator()
    {
        RuleFor(x => x.Numero).NotEmpty().WithMessage("Le numéro de vol est obligatoire.");
        RuleFor(x => x.Depart).NotEmpty().WithMessage("L'aéroport de départ est obligatoire.");
        RuleFor(x => x.Arrivee).NotEmpty().WithMessage("L'aéroport d'arrivée est obligatoire.");
        RuleFor(x => x.HeureDepart).NotEmpty().Matches(@"^\d{2}:\d{2}$")
            .WithMessage("L'heure de départ doit être au format HH:mm.");
        RuleFor(x => x.HeureArrivee).NotEmpty().Matches(@"^\d{2}:\d{2}$")
            .WithMessage("L'heure d'arrivée doit être au format HH:mm.");
    }
}

public class CreateVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<CreateVolCommand, VolDto>
{
    public async Task<VolDto> Handle(CreateVolCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var vol = new Vol
        {
            Numero = request.Numero,
            Depart = request.Depart,
            Arrivee = request.Arrivee,
            HeureDepart = request.HeureDepart,
            HeureArrivee = request.HeureArrivee,
            MH = request.MH
        };

        db.Vols.Add(vol);
        await db.SaveChangesAsync(cancellationToken);

        return vol.ToDto();
    }
}
