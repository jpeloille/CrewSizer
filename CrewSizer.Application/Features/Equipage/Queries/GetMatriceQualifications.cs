using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Queries;

public record GetMatriceQualificationsQuery(GroupeCheck? Groupe = null)
    : IRequest<MatriceQualificationsDto>;

public class GetMatriceQualificationsHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<GetMatriceQualificationsQuery, MatriceQualificationsDto>
{
    public async Task<MatriceQualificationsDto> Handle(GetMatriceQualificationsQuery request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var membres = await db.MembresEquipage.AsNoTracking()
            .Where(m => m.Actif)
            .OrderBy(m => m.Nom)
            .ToListAsync(cancellationToken);

        var checksQuery = db.DefinitionsCheck.AsNoTracking();
        if (request.Groupe.HasValue)
        {
            checksQuery = checksQuery.Where(d => d.Groupe == request.Groupe.Value);
            var contrat = request.Groupe == GroupeCheck.Cockpit ? TypeContrat.PNT : TypeContrat.PNC;
            membres = membres.Where(m => m.Contrat == contrat).ToList();
        }

        var definitions = await checksQuery.OrderBy(d => d.Code).ToListAsync(cancellationToken);
        var codes = definitions.Select(d => d.Code).ToList();

        var lignes = membres.Select(m => new MatriceLigneDto
        {
            MembreId = m.Id,
            Code = m.Code,
            Nom = m.Nom,
            Grade = m.Grade,
            Contrat = m.Contrat,
            Checks = codes.ToDictionary(
                code => code,
                code =>
                {
                    var q = m.Qualifications.FirstOrDefault(
                        q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase));
                    return new MatriceCellDto
                    {
                        CodeCheck = code,
                        Statut = q?.Statut ?? StatutCheck.NonApplicable,
                        DateExpiration = q?.DateExpiration
                    };
                })
        }).ToList();

        return new MatriceQualificationsDto
        {
            CodesChecks = codes,
            Lignes = lignes,
            FiltreGroupe = request.Groupe
        };
    }
}
