using CrewSizer.Domain.Enums;
using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Domain.Entities;

/// <summary>Données équipage complètes</summary>
public class DonneesEquipage
{
    public DateTime DateExtraction { get; set; }
    public List<MembreEquipage> Membres { get; set; } = [];
    public List<DefinitionCheck> Checks { get; set; } = [];
    public List<Competence> Competences { get; set; } = [];
    public List<AffectationEquipage> Affectations { get; set; } = [];
    public List<DisponibiliteMembre> Indisponibilites { get; set; } = [];
    public List<HistoriqueHDV> HistoriquesHDV { get; set; } = [];

    public int NbCdb => Membres.Count(m => m.Actif && m.Grade == Grade.CDB);
    public int NbOpl => Membres.Count(m => m.Actif && m.Grade == Grade.OPL);
    public int NbCc => Membres.Count(m => m.Actif && m.Grade == Grade.CC);
    public int NbPnc => Membres.Count(m => m.Actif && m.Grade == Grade.PNC);

    public Effectif CalculerEffectif() => new()
    {
        Cdb = NbCdb,
        Opl = NbOpl,
        Cc = NbCc,
        Pnc = NbPnc
    };

    /// <summary>Effectif opérationnel : actifs + compétents + disponibles sur la période</summary>
    public Effectif CalculerEffectifOperationnel(DateTime debutPeriode, DateTime finPeriode)
    {
        int cdb = 0, opl = 0, cc = 0, pnc = 0;
        var joursPeriode = (finPeriode.Date - debutPeriode.Date).Days + 1;

        foreach (var m in Membres.Where(m => m.Actif))
        {
            if (!EstCompetent(m)) continue;

            var joursIndispo = Indisponibilites
                .Where(i => i.MembreId == m.Id)
                .Sum(i => i.JoursChevauche(debutPeriode, finPeriode));

            if (joursIndispo > joursPeriode / 2) continue;

            switch (m.Grade)
            {
                case Grade.CDB: cdb++; break;
                case Grade.OPL: opl++; break;
                case Grade.CC: cc++; break;
                case Grade.PNC: pnc++; break;
            }
        }
        return new Effectif { Cdb = cdb, Opl = opl, Cc = cc, Pnc = pnc };
    }

    /// <summary>Liste des membres actifs exclus du calcul, avec la raison</summary>
    public List<(MembreEquipage Membre, string Raison)> MembresNonEngageables(DateTime debutPeriode, DateTime finPeriode)
    {
        var result = new List<(MembreEquipage, string)>();
        var joursPeriode = (finPeriode.Date - debutPeriode.Date).Days + 1;

        foreach (var m in Membres.Where(m => m.Actif))
        {
            if (!EstCompetent(m))
            {
                // Trouver les checks expirés pour le message
                var groupe = m.Contrat == TypeContrat.PNT ? GroupeCheck.Cockpit : GroupeCheck.Cabine;
                var checksExpires = Competences
                    .Where(c => c.Groupe == groupe)
                    .SelectMany(c => c.ChecksRequis)
                    .Where(code => m.Qualifications
                        .Any(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase)
                                  && q.Statut == StatutCheck.Expire))
                    .Distinct()
                    .ToList();

                var detail = checksExpires.Count > 0
                    ? string.Join(", ", checksExpires)
                    : "qualifications insuffisantes";
                result.Add((m, $"check(s) expiré(s) : {detail}"));
                continue;
            }

            var joursIndispo = Indisponibilites
                .Where(i => i.MembreId == m.Id)
                .Sum(i => i.JoursChevauche(debutPeriode, finPeriode));

            if (joursIndispo > joursPeriode / 2)
            {
                result.Add((m, $"indisponible {joursIndispo}/{joursPeriode} jours sur la période"));
            }
        }
        return result;
    }

    private bool EstCompetent(MembreEquipage membre)
    {
        var groupe = membre.Contrat == TypeContrat.PNT ? GroupeCheck.Cockpit : GroupeCheck.Cabine;
        var competencesGroupe = Competences.Where(c => c.Groupe == groupe).ToList();
        if (competencesGroupe.Count == 0) return true;
        return competencesGroupe.All(c => c.EstValide(membre));
    }
}
