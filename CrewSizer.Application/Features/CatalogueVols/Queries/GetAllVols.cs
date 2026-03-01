using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueVols.Queries;

public record GetAllVolsQuery : IRequest<List<VolDto>>;

public class GetAllVolsHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetAllVolsQuery, List<VolDto>>
{
    public async Task<List<VolDto>> Handle(GetAllVolsQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var vols = await db.Vols
            .AsNoTracking()
            .OrderBy(v => v.Numero)
            .ToListAsync(cancellationToken);

        return vols.Select(v => v.ToDto()).ToList();
    }
}
