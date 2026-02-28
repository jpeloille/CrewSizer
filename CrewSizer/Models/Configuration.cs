using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CrewSizer.Helpers;

namespace CrewSizer.Models;

public class Configuration
{
    public Periode Periode { get; set; } = new();
    public Effectif Effectif { get; set; } = new();
    public LimitesFTL LimitesFTL { get; set; } = new();
    public LimitesCumulatives LimitesCumulatives { get; set; } = new();
    public JoursOff JoursOff { get; set; } = new();
    public List<FonctionSol> FonctionsSolPNT { get; set; } = [];
    public List<FonctionSol> FonctionsSolPNC { get; set; } = [];
    public List<Abattement> AbattementsPNT { get; set; } = [];
    public List<Abattement> AbattementsPNC { get; set; } = [];
    public List<SemaineType> SemainesTypes { get; set; } = [];
    public List<AffectationSemaine> Calendrier { get; set; } = [];
    public List<Vol> CatalogueVols { get; set; } = [];
    public List<BlocVol> CatalogueBlocs { get; set; } = [];
    public List<EntreeTsvMax> TableTsvMax { get; set; } = [];
    public LimitesTempsService LimitesTempsService { get; set; } = new();
    public DonneesEquipage? Equipage { get; set; }
}

public class Periode
{
    public string Mois { get; set; } = "";
    public int Annee { get; set; }
    public int NbJours { get; set; }
}

public class Effectif
{
    public int Cdb { get; set; }
    public int Opl { get; set; }
    public int Cc { get; set; }
    public int Pnc { get; set; }
}

public class LimitesFTL
{
    public double TsvMaxJournalier { get; set; } = 13.0;
    public double TsvMoyenRetenu { get; set; } = 10.0;
    public double ReposMinimum { get; set; } = 12.0;
}

public class LimitesCumulatives
{
    public double H28Max { get; set; } = 100;
    public double H90Max { get; set; } = 280;
    public double H12Max { get; set; } = 900;
    public CumulEntrant CumulPNT { get; set; } = new();
    public CumulEntrant CumulPNC { get; set; } = new();
}

public class CumulEntrant
{
    public double Cumul28Entrant { get; set; }
    public double Cumul90Entrant { get; set; }
    public double Cumul12Entrant { get; set; }
}

public class JoursOff
{
    public int Reglementaire { get; set; } = 8;
    public int AccordEntreprise { get; set; } = 2;
}

public class FonctionSol
{
    public string Nom { get; set; } = "";
    public int NbPersonnes { get; set; }
    public int JoursSolMois { get; set; }

    public int JoursPersonnelSol => NbPersonnes * JoursSolMois;
}

public class Abattement
{
    public string Libelle { get; set; } = "";
    public int JoursPersonnel { get; set; }
}

// ── Modèle programme : Vol + BlocVol ──

public class Vol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Numero { get; set; } = "";
    public string Depart { get; set; } = "";
    public string Arrivee { get; set; } = "";
    public string HeureDepart { get; set; } = "";
    public string HeureArrivee { get; set; } = "";
    public bool MH { get; set; }

    public double HdvVol => HeureHelper.CalculerDuree(HeureDepart, HeureArrivee);
}

public class EtapeVol
{
    public int Position { get; set; }
    public Guid VolId { get; set; }
}

public class BlocVol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public int Sequence { get; set; }
    public string Jour { get; set; } = "";
    public string Periode { get; set; } = "";
    public string DebutDP { get; set; } = "";
    public string FinDP { get; set; } = "";
    public string DebutFDP { get; set; } = "";
    public string FinFDP { get; set; } = "";
    public List<EtapeVol> Etapes { get; set; } = [];
    public List<Vol> Vols { get; set; } = [];

    // ── Propriétés calculées ──

    public string Nom => DeriveNom();

    public int JourIndex => JourVersIndex(Jour);

    public int NbEtapes => Vols.Count;

    public double HdvBloc => Vols.Sum(v => v.HdvVol);

    public double DureeDPHeures => HeureHelper.CalculerDuree(DebutDP, FinDP);

    public double DureeFDPHeures => HeureHelper.CalculerDuree(DebutFDP, FinFDP);

    public double DureeTSHeures => DureeDPHeures;

    public double DureeTSVHeures => DureeFDPHeures;

    public string JourNom => Jour;

    private string DeriveNom()
    {
        if (Vols.Count == 0) return "";
        return Vols[0].Depart + string.Concat(Vols.Select(v => $"-{v.Arrivee}"));
    }

    public static int JourVersIndex(string jour) => HeureHelper.JourVersIndex(jour);

    public static string IndexVersJour(int idx) => HeureHelper.IndexVersJour(idx);

    public static TimeSpan ParseHeure(string hhmm) => HeureHelper.ParseHeure(hhmm);

    public static double CalculerDuree(string debut, string fin) => HeureHelper.CalculerDuree(debut, fin);
}

// ── Semaines types + Calendrier ──

public class BlocPlacement
{
    public Guid BlocId { get; set; }
    public string Jour { get; set; } = "";
    public int Sequence { get; set; }
}

public class SemaineType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    public string Saison { get; set; } = "";
    public List<BlocPlacement> Placements { get; set; } = [];
    public List<BlocVol> Blocs { get; set; } = [];
}

public class AffectationSemaine
{
    public int Semaine { get; set; }
    public int Annee { get; set; }
    public Guid SemaineTypeId { get; set; }
    public string SemaineTypeRef { get; set; } = "";
}

// ── Table TSV max EU-OPS ──

public class EntreeTsvMax
{
    /// <summary>Début de la bande horaire (inclusive), format "HH:mm"</summary>
    public string DebutBande { get; set; } = "";

    /// <summary>Fin de la bande horaire (exclusive), format "HH:mm"</summary>
    public string FinBande { get; set; } = "";

    /// <summary>TSV max en heures, indexé par nombre d'étapes. Clé = nb étapes, Valeur = max TSV heures.</summary>
    public Dictionary<int, double> MaxParEtapes { get; set; } = new();
}

// ── Limites temps de service ──

public class LimitesTempsService
{
    /// <summary>Max 60h de service sur 7 jours consécutifs</summary>
    public double Max7j { get; set; } = 60;

    /// <summary>Max 110h de service sur 14 jours consécutifs</summary>
    public double Max14j { get; set; } = 110;

    /// <summary>Max 190h de service sur 28 jours consécutifs</summary>
    public double Max28j { get; set; } = 190;
}
