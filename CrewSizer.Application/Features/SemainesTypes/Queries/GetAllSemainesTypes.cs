using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.SemainesTypes.Queries;

public record GetAllSemainesTypesQuery : IRequest<List<SemaineTypeDto>>;

public class GetAllSemainesTypesHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetAllSemainesTypesQuery, List<SemaineTypeDto>>
{
    public async Task<List<SemaineTypeDto>> Handle(GetAllSemainesTypesQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var semaines = await db.SemainesTypes
            .AsNoTracking()
            .OrderBy(s => s.Reference)
            .ToListAsync(cancellationToken);

        return semaines.Select(s => s.ToDto()).ToList();
    }
}
