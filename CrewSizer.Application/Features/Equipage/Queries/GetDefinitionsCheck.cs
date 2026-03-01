using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetDefinitionsCheckQuery(GroupeCheck? Groupe = null)
    : IRequest<List<DefinitionCheckDto>>;

public class GetDefinitionsCheckHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetDefinitionsCheckQuery, List<DefinitionCheckDto>>
{
    public async Task<List<DefinitionCheckDto>> Handle(GetDefinitionsCheckQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage.AsNoTracking()
            .Where(m => m.Actif).ToListAsync(cancellationToken);

        var query = db.DefinitionsCheck.AsNoTracking();
        if (request.Groupe.HasValue)
            query = query.Where(d => d.Groupe == request.Groupe.Value);

        var checks = await query.OrderBy(d => d.Code).ToListAsync(cancellationToken);
        return checks.Select(c => c.ToDto(membres)).ToList();
    }
}
