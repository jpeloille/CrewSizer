using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.TypesAvion.Queries;

public record GetAllTypesAvionQuery : IRequest<List<TypeAvionDto>>;

public class GetAllTypesAvionHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetAllTypesAvionQuery, List<TypeAvionDto>>
{
    public async Task<List<TypeAvionDto>> Handle(GetAllTypesAvionQuery request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var list = await db.TypesAvion
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ToListAsync(ct);

        return list.Select(t => t.ToDto()).ToList();
    }
}
