using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetMembresPourCheckQuery(string CodeCheck) : IRequest<List<AlerteQualificationDto>>;

public class GetMembresPourCheckHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetMembresPourCheckQuery, List<AlerteQualificationDto>>
{
    public async Task<List<AlerteQualificationDto>> Handle(GetMembresPourCheckQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage.AsNoTracking()
            .Where(m => m.Actif).ToListAsync(cancellationToken);

        var check = await db.DefinitionsCheck.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == request.CodeCheck, cancellationToken);

        return membres
            .Select(m =>
            {
                var q = m.Qualifications.FirstOrDefault(
                    q => q.CodeCheck.Equals(request.CodeCheck, StringComparison.OrdinalIgnoreCase));
                if (q is null) return null;
                return new AlerteQualificationDto
                {
                    MembreId = m.Id,
                    MembreCode = m.Code,
                    MembreNom = m.Nom,
                    Grade = m.Grade,
                    CodeCheck = request.CodeCheck,
                    DescriptionCheck = check?.Description ?? request.CodeCheck,
                    Statut = q.Statut,
                    DateExpiration = q.DateExpiration,
                    JoursRestants = q.DateExpiration.HasValue
                        ? (int)(q.DateExpiration.Value.Date - DateTime.Today).TotalDays
                        : null
                };
            })
            .Where(a => a is not null)
            .OrderBy(a => a!.Statut == StatutCheck.Expire ? 0 : 1)
            .ThenBy(a => a!.JoursRestants ?? int.MaxValue)
            .ToList()!;
    }
}
