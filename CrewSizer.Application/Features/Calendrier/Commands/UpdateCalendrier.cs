using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Calendrier.Commands;

public record UpdateCalendrierCommand(
    Guid ScenarioId,
    List<AffectationSemaineDto> Affectations
) : IRequest<CalendrierDto>;

public class UpdateCalendrierValidator : AbstractValidator<UpdateCalendrierCommand>
{
    public UpdateCalendrierValidator()
    {
        RuleFor(x => x.ScenarioId).NotEmpty();
    }
}

public class UpdateCalendrierHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<UpdateCalendrierCommand, CalendrierDto>
{
    public async Task<CalendrierDto> Handle(UpdateCalendrierCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var scenario = await db.Scenarios.FindAsync([request.ScenarioId], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario '{request.ScenarioId}' introuvable.");

        // Vérifier que les semaines types référencées existent
        var stIds = request.Affectations.Select(a => a.SemaineTypeId).Distinct().ToList();
        if (stIds.Count > 0)
        {
            var existantes = await db.SemainesTypes
                .Where(s => stIds.Contains(s.Id))
                .CountAsync(cancellationToken);
            if (existantes != stIds.Count)
                throw new KeyNotFoundException("Une ou plusieurs semaines types référencées n'existent pas.");
        }

        scenario.Calendrier = request.Affectations.Select(a => new AffectationSemaine
        {
            Semaine = a.Semaine,
            Annee = a.Annee,
            SemaineTypeId = a.SemaineTypeId,
            SemaineTypeRef = a.SemaineTypeRef
        }).ToList();
        scenario.DateModification = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new CalendrierDto
        {
            ScenarioId = scenario.Id,
            Affectations = scenario.Calendrier.Select(c =>
                new AffectationSemaineDto(c.Semaine, c.Annee, c.SemaineTypeId, c.SemaineTypeRef)).ToList()
        };
    }
}
