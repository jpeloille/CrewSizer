using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Calcul.Queries;

public record GetAllSnapshotsQuery(Guid? ScenarioId = null) : IRequest<List<CalculSnapshotListItemDto>>;

public class GetAllSnapshotsHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetAllSnapshotsQuery, List<CalculSnapshotListItemDto>>
{
    public async Task<List<CalculSnapshotListItemDto>> Handle(GetAllSnapshotsQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Snapshots.AsNoTracking().AsQueryable();

        if (request.ScenarioId.HasValue)
            query = query.Where(s => s.ScenarioId == request.ScenarioId.Value);

        var snapshots = await query
            .OrderByDescending(s => s.DateCalcul)
            .ToListAsync(cancellationToken);

        return snapshots.Select(s => s.ToListItemDto()).ToList();
    }
}
