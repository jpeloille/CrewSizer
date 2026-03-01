using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetEquipageKpiQuery : IRequest<EquipageKpiDto>;

public class GetEquipageKpiHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetEquipageKpiQuery, EquipageKpiDto>
{
    public async Task<EquipageKpiDto> Handle(GetEquipageKpiQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage.AsNoTracking().ToListAsync(cancellationToken);
        var actifs = membres.Where(m => m.Actif).ToList();
        var toutesQualifs = actifs.SelectMany(m => m.Qualifications).ToList();

        return new EquipageKpiDto
        {
            TotalMembres = membres.Count,
            TotalActifs = actifs.Count,
            Cdb = actifs.Count(m => m.Grade == Grade.CDB),
            Opl = actifs.Count(m => m.Grade == Grade.OPL),
            Cc = actifs.Count(m => m.Grade == Grade.CC),
            Pnc = actifs.Count(m => m.Grade == Grade.PNC),
            AlertesExpirees = toutesQualifs.Count(q => q.Statut == StatutCheck.Expire),
            AlertesProches = toutesQualifs.Count(q => q.Statut == StatutCheck.ExpirationProche),
            AlertesAvertissement = toutesQualifs.Count(q => q.Statut == StatutCheck.Avertissement)
        };
    }
}
