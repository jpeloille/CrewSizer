using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.CatalogueBlocs.Queries;

public record GetAllBlocsVolQuery : IRequest<List<BlocVolDto>>;

public class GetAllBlocsVolHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetAllBlocsVolQuery, List<BlocVolDto>>
{
    public async Task<List<BlocVolDto>> Handle(GetAllBlocsVolQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var blocs = await db.BlocsVol
            .AsNoTracking()
            .Include(b => b.BlocType)
            .Include(b => b.TypeAvion)
            .OrderBy(b => b.Code)
            .ToListAsync(cancellationToken);

        // Hydrater les vols pour les propriétés calculées
        var allVolIds = blocs.SelectMany(b => b.Etapes.Select(e => e.VolId)).Distinct().ToList();
        var vols = await db.Vols
            .AsNoTracking()
            .Where(v => allVolIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var volsDict = vols.ToDictionary(v => v.Id);
        foreach (var bloc in blocs)
        {
            bloc.Vols = bloc.Etapes
                .OrderBy(e => e.Position)
                .Where(e => volsDict.ContainsKey(e.VolId))
                .Select(e => volsDict[e.VolId])
                .ToList();
        }

        return blocs.Select(b => b.ToDto()).ToList();
    }
}
