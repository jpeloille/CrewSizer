using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetMembresQuery : IRequest<QualificationMatrixDto>;

public class GetMembresHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<GetMembresQuery, QualificationMatrixDto>
{
    public async Task<QualificationMatrixDto> Handle(GetMembresQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage
            .AsNoTracking()
            .OrderBy(m => m.Nom)
            .ToListAsync(cancellationToken);

        var actifs = membres.Where(m => m.Actif).ToList();

        return new QualificationMatrixDto
        {
            TotalMembres = membres.Count,
            Cdb = actifs.Count(m => m.Grade == Domain.Enums.Grade.CDB),
            Opl = actifs.Count(m => m.Grade == Domain.Enums.Grade.OPL),
            Cc = actifs.Count(m => m.Grade == Domain.Enums.Grade.CC),
            Pnc = actifs.Count(m => m.Grade == Domain.Enums.Grade.PNC),
            Membres = membres.Select(m => m.ToDto()).ToList()
        };
    }
}
