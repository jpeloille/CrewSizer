using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetAlertesQualificationsQuery : IRequest<List<AlerteQualificationDto>>;

public class GetAlertesQualificationsHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetAlertesQualificationsQuery, List<AlerteQualificationDto>>
{
    public async Task<List<AlerteQualificationDto>> Handle(GetAlertesQualificationsQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage.AsNoTracking()
            .Where(m => m.Actif).ToListAsync(cancellationToken);
        var checks = await db.DefinitionsCheck.AsNoTracking().ToListAsync(cancellationToken);
        var checkLookup = checks.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var alertes = new List<AlerteQualificationDto>();
        foreach (var m in membres)
        {
            foreach (var q in m.Qualifications.Where(q =>
                         q.Statut is StatutCheck.Expire or StatutCheck.Avertissement or StatutCheck.ExpirationProche))
            {
                var desc = checkLookup.GetValueOrDefault(q.CodeCheck);
                alertes.Add(new AlerteQualificationDto
                {
                    MembreId = m.Id,
                    MembreCode = m.Code,
                    MembreNom = m.Nom,
                    Grade = m.Grade,
                    CodeCheck = q.CodeCheck,
                    DescriptionCheck = desc?.Description ?? q.CodeCheck,
                    Statut = q.Statut,
                    DateExpiration = q.DateExpiration,
                    JoursRestants = q.DateExpiration.HasValue
                        ? (int)(q.DateExpiration.Value.Date - DateTime.Today).TotalDays
                        : null
                });
            }
        }

        return alertes
            .OrderBy(a => a.Statut == StatutCheck.Expire ? 0 : 1)
            .ThenBy(a => a.JoursRestants ?? int.MaxValue)
            .ToList();
    }
}
