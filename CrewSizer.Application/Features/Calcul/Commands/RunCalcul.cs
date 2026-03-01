using System.Text.Json;
using System.Text.Json.Serialization;
using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Domain.Services;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Calcul.Commands;

public record RunCalculCommand(Guid ScenarioId, string? CalculePar = null) : IRequest<CalculSnapshotDto>;

public class RunCalculHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<RunCalculCommand, CalculSnapshotDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public async Task<CalculSnapshotDto> Handle(RunCalculCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        // 1. Charger le scénario
        var scenario = await db.Scenarios.FindAsync([request.ScenarioId], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario '{request.ScenarioId}' introuvable.");

        // 2. Charger les catalogues complets
        var vols = await db.Vols.AsNoTracking().ToListAsync(cancellationToken);
        var blocs = await db.BlocsVol.AsNoTracking()
            .Include(b => b.TypeAvion)
            .ToListAsync(cancellationToken);
        var semaines = await db.SemainesTypes.AsNoTracking().ToListAsync(cancellationToken);

        // 3. Charger l'équipage si existant
        var membres = await db.MembresEquipage.AsNoTracking().ToListAsync(cancellationToken);
        var checks = await db.DefinitionsCheck.AsNoTracking().ToListAsync(cancellationToken);
        DonneesEquipage? equipage = null;
        if (membres.Count > 0)
        {
            equipage = new DonneesEquipage
            {
                Membres = membres,
                Checks = checks,
                Competences = await db.Competences.AsNoTracking().ToListAsync(cancellationToken),
                Indisponibilites = await db.DisponibilitesMembre.AsNoTracking().ToListAsync(cancellationToken),
                HistoriquesHDV = await db.HistoriquesHDV.AsNoTracking().ToListAsync(cancellationToken)
            };
        }

        // 4. Configuration.FromScenario → pont vers CalculateurMarge inchangé
        var config = Configuration.FromScenario(scenario, vols, blocs, semaines, equipage);

        // 5. Résoudre les catalogues (hydrater Vols, Blocs, Semaines)
        CatalogueResolver.ResoudreTout(config);

        // 6. Calculer
        var resultat = CalculateurMarge.Calculer(config);

        // 6a. Effectif total vs opérationnel + alertes membres non engageables
        if (equipage != null)
        {
            resultat.EffectifTotal = equipage.CalculerEffectif();
            var debut = scenario.Periode.DateDebut.ToDateTime(TimeOnly.MinValue);
            var fin = scenario.Periode.DateFin.ToDateTime(TimeOnly.MinValue);
            foreach (var (membre, raison) in equipage.MembresNonEngageables(debut, fin))
            {
                resultat.Alertes.Add($"ALERTE: {membre.Grade} {membre.Nom} non engageable — {raison}");
            }
        }

        // 6b. Ventilation par mois si la période couvre > 1 mois
        if (config.Periode.MoisCouverts().Count > 1)
            resultat.ResultatsParMois = CalculateurMarge.CalculerParMois(config);

        // 7. Sérialiser et persister le snapshot
        var snapshot = new CalculSnapshot
        {
            ScenarioId = scenario.Id,
            CalculePar = request.CalculePar,
            TauxEngagementGlobal = resultat.TauxEngagementGlobal,
            StatutGlobal = resultat.StatutGlobal,
            CategorieContraignante = resultat.CategorieContraignante,
            TotalBlocs = resultat.TotalBlocs,
            TotalHDV = resultat.TotalHDV,
            Rotations = resultat.Rotations,
            ResultatJson = JsonSerializer.Serialize(resultat, JsonOptions)
        };

        db.Snapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);

        return snapshot.ToDto();
    }
}
