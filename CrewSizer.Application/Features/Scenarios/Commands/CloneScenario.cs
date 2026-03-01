using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Domain.ValueObjects;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Commands;

public record CloneScenarioCommand(Guid SourceId, string NouveauNom, string? CreePar) : IRequest<ScenarioDto>;

public class CloneScenarioHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<CloneScenarioCommand, ScenarioDto>
{
    public async Task<ScenarioDto> Handle(CloneScenarioCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var source = await db.Scenarios.FindAsync([request.SourceId], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario source '{request.SourceId}' introuvable.");

        var clone = new ConfigurationScenario
        {
            Nom = request.NouveauNom,
            Description = $"Copie de '{source.Nom}'",
            CreePar = request.CreePar,
            ModifiePar = request.CreePar,
            Periode = new Periode { DateDebut = source.Periode.DateDebut, DateFin = source.Periode.DateFin },
            Effectif = new Effectif { Cdb = source.Effectif.Cdb, Opl = source.Effectif.Opl, Cc = source.Effectif.Cc, Pnc = source.Effectif.Pnc },
            LimitesFTL = new LimitesFTL
            {
                TsvMaxJournalier = source.LimitesFTL.TsvMaxJournalier,
                TsvMoyenRetenu = source.LimitesFTL.TsvMoyenRetenu,
                ReposMinimum = source.LimitesFTL.ReposMinimum
            },
            LimitesCumulatives = new LimitesCumulatives
            {
                H28Max = source.LimitesCumulatives.H28Max,
                H90Max = source.LimitesCumulatives.H90Max,
                H12Max = source.LimitesCumulatives.H12Max,
                CumulPNT = new CumulEntrant
                {
                    Cumul28Entrant = source.LimitesCumulatives.CumulPNT.Cumul28Entrant,
                    Cumul90Entrant = source.LimitesCumulatives.CumulPNT.Cumul90Entrant,
                    Cumul12Entrant = source.LimitesCumulatives.CumulPNT.Cumul12Entrant
                },
                CumulPNC = new CumulEntrant
                {
                    Cumul28Entrant = source.LimitesCumulatives.CumulPNC.Cumul28Entrant,
                    Cumul90Entrant = source.LimitesCumulatives.CumulPNC.Cumul90Entrant,
                    Cumul12Entrant = source.LimitesCumulatives.CumulPNC.Cumul12Entrant
                }
            },
            JoursOff = new JoursOff
            {
                Reglementaire = source.JoursOff.Reglementaire,
                AccordEntreprise = source.JoursOff.AccordEntreprise
            },
            LimitesTempsService = new LimitesTempsService
            {
                Max7j = source.LimitesTempsService.Max7j,
                Max14j = source.LimitesTempsService.Max14j,
                Max28j = source.LimitesTempsService.Max28j
            },
            FonctionsSolPNT = source.FonctionsSolPNT.Select(f => new FonctionSol
                { Nom = f.Nom, NbPersonnes = f.NbPersonnes, JoursSolMois = f.JoursSolMois }).ToList(),
            FonctionsSolPNC = source.FonctionsSolPNC.Select(f => new FonctionSol
                { Nom = f.Nom, NbPersonnes = f.NbPersonnes, JoursSolMois = f.JoursSolMois }).ToList(),
            AbattementsPNT = source.AbattementsPNT.Select(a => new Abattement
                { Libelle = a.Libelle, JoursPersonnel = a.JoursPersonnel }).ToList(),
            AbattementsPNC = source.AbattementsPNC.Select(a => new Abattement
                { Libelle = a.Libelle, JoursPersonnel = a.JoursPersonnel }).ToList(),
            TableTsvMax = source.TableTsvMax.Select(t => new EntreeTsvMax
                { DebutBande = t.DebutBande, FinBande = t.FinBande, MaxParEtapes = new Dictionary<int, double>(t.MaxParEtapes) }).ToList(),
            Calendrier = source.Calendrier.Select(c => new AffectationSemaine
                { Semaine = c.Semaine, Annee = c.Annee, SemaineTypeId = c.SemaineTypeId, SemaineTypeRef = c.SemaineTypeRef }).ToList()
        };

        db.Scenarios.Add(clone);
        await db.SaveChangesAsync(cancellationToken);

        return clone.ToDto();
    }
}
