using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Queries;

public record GetAllScenariosQuery : IRequest<List<ScenarioListItemDto>>;

public class GetAllScenariosHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetAllScenariosQuery, List<ScenarioListItemDto>>
{
    public async Task<List<ScenarioListItemDto>> Handle(GetAllScenariosQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Scenarios
            .AsNoTracking()
            .OrderByDescending(s => s.DateModification)
            .Select(s => new ScenarioListItemDto
            {
                Id = s.Id,
                Nom = s.Nom,
                Description = s.Description,
                DateModification = s.DateModification,
                ModifiePar = s.ModifiePar,
                DateDebut = s.Periode.DateDebut,
                DateFin = s.Periode.DateFin
            })
            .ToListAsync(cancellationToken);
    }
}
