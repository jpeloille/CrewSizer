using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetMembreDetailQuery(Guid MembreId) : IRequest<MembreDetailDto?>;

public class GetMembreDetailHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetMembreDetailQuery, MembreDetailDto?>
{
    public async Task<MembreDetailDto?> Handle(GetMembreDetailQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membre = await db.MembresEquipage
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MembreId, cancellationToken);

        return membre?.ToDetailDto();
    }
}
