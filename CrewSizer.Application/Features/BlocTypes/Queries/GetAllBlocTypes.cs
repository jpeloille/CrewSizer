using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.BlocTypes.Queries;

public record GetAllBlocTypesQuery : IRequest<List<BlocTypeDto>>;

public class GetAllBlocTypesHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetAllBlocTypesQuery, List<BlocTypeDto>>
{
    public async Task<List<BlocTypeDto>> Handle(GetAllBlocTypesQuery request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var list = await db.BlocTypes
            .AsNoTracking()
            .OrderBy(bt => bt.Code)
            .ToListAsync(ct);

        return list.Select(bt => bt.ToDto()).ToList();
    }
}
