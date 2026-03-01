using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Queries;

public record GetScenarioByIdQuery(Guid Id) : IRequest<ScenarioDto?>;

public class GetScenarioByIdHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetScenarioByIdQuery, ScenarioDto?>
{
    public async Task<ScenarioDto?> Handle(GetScenarioByIdQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var scenario = await db.Scenarios.FindAsync([request.Id], cancellationToken);
        return scenario?.ToDto();
    }
}
