using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Domain.Entities;

public class ResultatMarge
{
    // Période
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }
    public string LibellePeriode { get; set; } = "";
    public int NbJours { get; set; }

    // Effectif utilisé pour le calcul
    public Effectif EffectifUtilise { get; set; } = new();

    // Disponibilité commune
    public int DDisponible { get; set; }
    public int JoursServiceMaxCycle { get; set; }
    public double Cycle { get; set; }

    // Groupes
    public ResultatGroupe PNT { get; set; } = new();
    public ResultatGroupe PNC { get; set; } = new();

    // Sous-catégories
    public ResultatCategorie CDB { get; set; } = new();
    public ResultatCategorie OPL { get; set; } = new();
    public ResultatCategorie CC { get; set; } = new();
    public ResultatCategorie PNCDetail { get; set; } = new();

    // Programme
    public int TotalBlocs { get; set; }
    public double TotalHDV { get; set; }
    public int Rotations { get; set; }
    public double EtapesParRotation { get; set; }

    // Allocation cabine
    public int RotationsAvecPNC { get; set; }
    public int RotationsSansPNC { get; set; }

    // Global
    public string CategorieContraignante { get; set; } = "";
    public double TauxEngagementGlobal { get; set; }
    public string StatutGlobal { get; set; } = "";

    // Analyses
    public int NMinPNT { get; set; }
    public int NMinPNCGroupe { get; set; }
    public int ExcedentPNT { get; set; }
    public int ExcedentPNCGroupe { get; set; }
    public int BlocsAbsorbables { get; set; }

    // Programme détaillé
    public List<(string nom, int blocs, double hdv)> DetailProgramme { get; set; } = [];

    // Vérifications FTL
    public List<VerifTsvMax> VerificationsTSV { get; set; } = [];
    public bool TousBlocsConformesTSV { get; set; } = true;
    public VerifTempsService VerifTempsServicePNT { get; set; } = new();
    public VerifTempsService VerifTempsServicePNC { get; set; } = new();
    public List<ResumeSemaineJour> ResumeSemaine { get; set; } = [];
    public double SemainesMois { get; set; }
    public int NbSemainesPeriode { get; set; }

    // Ventilation mensuelle
    public List<ResultatMarge>? ResultatsParMois { get; set; }

    // Alertes
    public List<string> Alertes { get; set; } = [];
}

public class ResultatGroupe
{
    public string Nom { get; set; } = "";
    public int Effectif { get; set; }
    public int CapaciteBrute { get; set; }
    public int TotalAbattements { get; set; }
    public int TotalJoursSol { get; set; }
    public int CapaciteNette { get; set; }
    public double Alpha { get; set; }

    // Contraintes cumulatives
    public double HMax { get; set; }
    public string ContrainteMordante { get; set; } = "";
    public double CapaciteNetteHDV { get; set; }

    // Vérifications butées
    public double HdvParPersonne { get; set; }
    public (double cumul, bool ok) Verif28j { get; set; }
    public (double cumul, bool ok) Verif90j { get; set; }
    public (double cumul, bool ok) Verif12m { get; set; }
}

public class ResultatCategorie
{
    public string Nom { get; set; } = "";
    public int Effectif { get; set; }
    public double Capacite { get; set; }
    public int Besoin { get; set; }
    public double Marge { get; set; }
    public double TauxEngagement { get; set; }
    public string Statut { get; set; } = "";
}

/// <summary>Vérification TSV max par bloc de vol</summary>
public class VerifTsvMax
{
    public string Nom { get; set; } = "";
    public int Jour { get; set; }
    public string JourNom { get; set; } = "";
    public int NbEtapes { get; set; }
    public double TsvDuree { get; set; }
    public double TsvMaxAutorise { get; set; }
    public bool Conforme { get; set; }
}

/// <summary>Vérification des limites de temps de service par groupe</summary>
public class VerifTempsService
{
    public double TotalTSHebdo { get; set; }
    public double TotalTSMensuel { get; set; }
    public double TSParPersonneHebdo { get; set; }
    public (double valeur, double limite, bool ok) Verif7j { get; set; }
    public (double valeur, double limite, bool ok) Verif14j { get; set; }
    public (double valeur, double limite, bool ok) Verif28j { get; set; }
}

/// <summary>Résumé par jour de semaine</summary>
public class ResumeSemaineJour
{
    public int Jour { get; set; }
    public string JourNom { get; set; } = "";
    public int NbBlocs { get; set; }
    public double Hdv { get; set; }
    public double Ts { get; set; }
}
