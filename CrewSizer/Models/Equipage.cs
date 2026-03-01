namespace CrewSizer.Models;

// ── Enums ──

public enum TypeContrat { PNT, PNC }

public enum Grade { CDB, OPL, CC, PNC }

public enum StatutCheck { Valide, ExpirationProche, Avertissement, Expire, NonApplicable }

public enum GroupeCheck { Cockpit, Cabine }

public enum TypeCompetence { Qualification, Licence, Medical, Securite, Reglementaire }

public enum TypeActivite { Vol, Reserve, Formation, Sol, Repos, Conge }

public enum MotifIndisponibilite { CongeAnnuel, CongeMaladie, Maternite, Formation, Detachement, SansSolde, Suspension, Autre }

// ── Entités ──

/// <summary>Membre d'équipage (PNT ou PNC)</summary>
public class MembreEquipage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Nom { get; set; } = "";
    public bool Actif { get; set; } = true;
    public TypeContrat Contrat { get; set; }
    public Grade Grade { get; set; }
    public string Matricule { get; set; } = "";
    public DateTime? DateEntree { get; set; }
    public DateTime? DateFin { get; set; }
    public List<string> Roles { get; set; } = [];
    public string Categorie { get; set; } = "";
    public List<string> ReglesApplicables { get; set; } = [];
    public List<string> Bases { get; set; } = [];
    public string TypeAvion { get; set; } = "";
    public List<StatutQualification> Qualifications { get; set; } = [];
}

/// <summary>Statut d'une qualification pour un membre</summary>
public class StatutQualification
{
    public string CodeCheck { get; set; } = "";
    public DateTime? DateExpiration { get; set; }
    public StatutCheck Statut { get; set; } = StatutCheck.NonApplicable;
}

/// <summary>Définition d'un type de check/qualification</summary>
public class DefinitionCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Primaire { get; set; }
    public GroupeCheck Groupe { get; set; }
    public int ValiditeNombre { get; set; }
    public string ValiditeUnite { get; set; } = "";
    public bool FinDeMois { get; set; }
    public bool FinDAnnee { get; set; }
    public int RenouvellementNombre { get; set; }
    public string RenouvellementUnite { get; set; } = "";
    public int AvertissementNombre { get; set; }
    public string AvertissementUnite { get; set; } = "";
}

/// <summary>Compétence composite regroupant plusieurs checks requis</summary>
public class Competence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public TypeCompetence Type { get; set; } = TypeCompetence.Qualification;
    public GroupeCheck Groupe { get; set; }
    public List<string> ChecksRequis { get; set; } = [];

    /// <summary>Vérifie si un membre possède cette compétence (tous les checks requis non expirés)</summary>
    public bool EstValide(MembreEquipage membre)
    {
        if (ChecksRequis.Count == 0) return false;
        return ChecksRequis.All(code => membre.Qualifications
            .Any(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase)
                      && q.Statut != StatutCheck.Expire));
    }

    /// <summary>Statut agrégé : pire statut parmi les checks requis</summary>
    public StatutCheck StatutPour(MembreEquipage membre)
    {
        if (ChecksRequis.Count == 0) return StatutCheck.NonApplicable;

        var pire = StatutCheck.Valide;
        foreach (var code in ChecksRequis)
        {
            var qualif = membre.Qualifications
                .FirstOrDefault(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (qualif == null) return StatutCheck.Expire;
            if (qualif.Statut > pire) pire = qualif.Statut;
        }
        return pire;
    }

    /// <summary>Prochaine date d'expiration parmi les checks requis</summary>
    public DateTime? ProchaineExpiration(MembreEquipage membre)
    {
        DateTime? min = null;
        foreach (var code in ChecksRequis)
        {
            var qualif = membre.Qualifications
                .FirstOrDefault(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (qualif?.DateExpiration != null && (min == null || qualif.DateExpiration < min))
                min = qualif.DateExpiration;
        }
        return min;
    }
}

/// <summary>Affectation d'un membre d'équipage à une activité sur une date</summary>
public class AffectationEquipage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public DateTime Date { get; set; }
    public TypeActivite Activite { get; set; } = TypeActivite.Vol;
    public Guid BlocId { get; set; }
    public string BlocCode { get; set; } = "";
    public string Commentaire { get; set; } = "";
    public double HeuresVol { get; set; }
    public double TempsService { get; set; }

    public bool EstProductif => Activite is TypeActivite.Vol or TypeActivite.Reserve;
    public bool EstIndisponible => Activite is TypeActivite.Repos or TypeActivite.Conge;
}

/// <summary>Période d'indisponibilité d'un membre d'équipage</summary>
public class DisponibiliteMembre
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public string MembreCode { get; set; } = "";
    public MotifIndisponibilite Motif { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string Commentaire { get; set; } = "";

    public int NbJours => DateFin.HasValue ? (DateFin.Value.Date - DateDebut.Date).Days + 1 : 0;

    public bool CouvreDate(DateTime date) =>
        date.Date >= DateDebut.Date && (!DateFin.HasValue || date.Date <= DateFin.Value.Date);

    /// <summary>Nombre de jours chevauchant une période donnée</summary>
    public int JoursChevauche(DateTime debutPeriode, DateTime finPeriode)
    {
        var debut = DateDebut.Date < debutPeriode.Date ? debutPeriode.Date : DateDebut.Date;
        var fin = DateFin.HasValue
            ? (DateFin.Value.Date > finPeriode.Date ? finPeriode.Date : DateFin.Value.Date)
            : finPeriode.Date;
        return debut > fin ? 0 : (fin - debut).Days + 1;
    }

    public bool EstActive => !DateFin.HasValue || DateFin.Value.Date >= DateTime.Today;

    public string MotifLibelle => Motif switch
    {
        MotifIndisponibilite.CongeAnnuel => "Conge annuel",
        MotifIndisponibilite.CongeMaladie => "Maladie",
        MotifIndisponibilite.Maternite => "Maternite",
        MotifIndisponibilite.Formation => "Formation",
        MotifIndisponibilite.Detachement => "Detachement",
        MotifIndisponibilite.SansSolde => "Sans solde",
        MotifIndisponibilite.Suspension => "Suspension",
        _ => "Autre"
    };
}

/// <summary>Historique des heures de vol individuelles d'un membre</summary>
public class HistoriqueHDV
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public string MembreCode { get; set; } = "";
    public DateTime DateReleve { get; set; }
    public double Cumul28j { get; set; }
    public double Cumul90j { get; set; }
    public double Cumul12m { get; set; }

    public double Marge28j(double limite = 100) => limite - Cumul28j;
    public double Marge90j(double limite = 280) => limite - Cumul90j;
    public double Marge12m(double limite = 900) => limite - Cumul12m;

    /// <summary>Heures volables max avant la prochaine butée</summary>
    public double HeuresDisponibles(double l28 = 100, double l90 = 280, double l12 = 900)
        => Math.Min(Marge28j(l28), Math.Min(Marge90j(l90), Marge12m(l12)));

    /// <summary>Contrainte mordante individuelle</summary>
    public string ContrainteMordante(double l28 = 100, double l90 = 280, double l12 = 900)
    {
        var m28 = Marge28j(l28);
        var m90 = Marge90j(l90);
        var m12 = Marge12m(l12);
        var min = Math.Min(m28, Math.Min(m90, m12));
        if (min == m28) return "28 jours";
        if (min == m90) return "90 jours";
        return "12 mois";
    }

    public (double cumul, double limite, bool ok) Verif28j(double hdvSupp, double l = 100)
        => (Cumul28j + hdvSupp, l, Cumul28j + hdvSupp <= l);
    public (double cumul, double limite, bool ok) Verif90j(double hdvSupp, double l = 280)
        => (Cumul90j + hdvSupp, l, Cumul90j + hdvSupp <= l);
    public (double cumul, double limite, bool ok) Verif12m(double hdvSupp, double l = 900)
        => (Cumul12m + hdvSupp, l, Cumul12m + hdvSupp <= l);
}

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

    private bool EstCompetent(MembreEquipage membre)
    {
        var groupe = membre.Contrat == TypeContrat.PNT ? GroupeCheck.Cockpit : GroupeCheck.Cabine;
        var competencesGroupe = Competences.Where(c => c.Groupe == groupe).ToList();
        if (competencesGroupe.Count == 0) return true;
        return competencesGroupe.All(c => c.EstValide(membre));
    }
}
