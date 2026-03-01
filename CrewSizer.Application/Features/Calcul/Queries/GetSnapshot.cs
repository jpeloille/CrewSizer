using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Calcul.Queries;

public record GetSnapshotQuery(Guid Id) : IRequest<CalculSnapshotDto?>;

public class GetSnapshotHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetSnapshotQuery, CalculSnapshotDto?>
{
    public async Task<CalculSnapshotDto?> Handle(GetSnapshotQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var snapshot = await db.Snapshots.FindAsync([request.Id], cancellationToken);
        return snapshot?.ToDto();
    }
}
