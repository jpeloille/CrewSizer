using CrewSizer.Application.Common.Dtos;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Calendrier.Queries;

public record GetCalendrierQuery(Guid ScenarioId) : IRequest<CalendrierDto?>;

public class GetCalendrierHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetCalendrierQuery, CalendrierDto?>
{
    public async Task<CalendrierDto?> Handle(GetCalendrierQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var scenario = await db.Scenarios.FindAsync([request.ScenarioId], cancellationToken);
        if (scenario is null) return null;

        return new CalendrierDto
        {
            ScenarioId = scenario.Id,
            Affectations = scenario.Calendrier.Select(c =>
                new AffectationSemaineDto(c.Semaine, c.Annee, c.SemaineTypeId, c.SemaineTypeRef)).ToList()
        };
    }
}
