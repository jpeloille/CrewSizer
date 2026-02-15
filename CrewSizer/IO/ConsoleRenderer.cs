using System.Text;
using CrewSizer.Models;

namespace CrewSizer.IO;

public static class ConsoleRenderer
{
    private const int W = 66;

    public static void Afficher(ResultatMarge r, Configuration config)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();

        DoubleLigne();
        Centre($"MARGE D'ENGAGEMENT EQUIPAGE -- {r.Mois} {r.Annee} ({r.NbJours} jours)");
        DoubleLigne();

        // ── EFFECTIF ──
        Section("EFFECTIF");
        Console.WriteLine("  PNT");
        Ligne("    CDB", r.CDB.Effectif.ToString());
        Ligne("    OPL", r.OPL.Effectif.ToString());
        Ligne("    Total PNT", $"{r.PNT.Effectif} pilotes");
        Console.WriteLine("  PNC");
        Ligne("    CC", r.CC.Effectif.ToString());
        Ligne("    PNC", r.PNCDetail.Effectif.ToString());
        Ligne("    Total PNC", $"{r.PNC.Effectif}");
        Ligne("  Equipage / vol", "4  (1 CDB + 1 OPL + 1 CC + 1 PNC/CC)");

        // ── DISPONIBILITÉ ──
        Section("DISPONIBILITE");
        Ligne("  D_dispo (j engageables)", $"{r.DDisponible} j/personne");
        Ligne("  Cycle TSV+repos", $"{r.Cycle:F1} h");

        // ── CAPACITÉ PNT ──
        AfficherCapaciteGroupe(r.PNT);

        // ── CAPACITÉ PNC ──
        if (r.PNC.Effectif > 0)
            AfficherCapaciteGroupe(r.PNC);

        // ── VÉRIFICATIONS CUMULATIVES ──
        var lim = config.LimitesCumulatives;
        Section("VERIFICATIONS CUMULATIVES");
        AfficherVerifButees("  PNT", r.PNT, lim);
        if (r.PNC.Effectif > 0)
            AfficherVerifButees("  PNC", r.PNC, lim);

        // ── BESOIN OPÉRATIONNEL ──
        Section("BESOIN OPERATIONNEL");
        Ligne("  Blocs / mois", r.TotalBlocs.ToString());
        Ligne("  HDV / mois", $"{r.TotalHDV:F1} HDV");
        Ligne("  Rotations", r.Rotations.ToString());
        Ligne("  s (etapes/rotation)", $"{r.EtapesParRotation:F0}");
        Ligne("  Composition cabine",
            $"{r.RotationsAvecPNC} x (CC+PNC) | {r.RotationsSansPNC} x (2xCC)");

        // ── MARGE PAR CATÉGORIE ──
        Console.WriteLine();
        Console.WriteLine("  MARGE PAR CATEGORIE");
        DoubleLigne();
        Console.WriteLine(
            $"  {"Cat.",-6}{"Eff.",6}{"Capacite",10}{"Besoin",8}{"Marge",9}{"Tau",9}  Statut");
        AfficherLigneCategorie(r.CDB);
        AfficherLigneCategorie(r.OPL);
        AfficherLigneCategorie(r.CC);
        if (r.PNCDetail.Effectif > 0)
            AfficherLigneCategorie(r.PNCDetail);
        DoubleLigne();

        // ── RÉSULTAT GLOBAL ──
        Console.WriteLine();
        Console.WriteLine("  RESULTAT GLOBAL");
        DoubleLigne();
        string icone = IconeStatut(r.StatutGlobal);
        Console.WriteLine($"  | CATEGORIE CONTRAIGNANTE ... {r.CategorieContraignante,-30}|");
        Console.WriteLine($"  | TAUX D'ENGAGEMENT ........ {r.TauxEngagementGlobal,6:P1}{"",24}|");
        Console.WriteLine($"  | STATUT ................... {icone} {r.StatutGlobal,-25}|");
        DoubleLigne();

        // ── ANALYSES ──
        Section("ANALYSES");
        Ligne("  Effectif min PNT",
            $"{r.NMinPNT} pilotes (excedent {SigneVal(r.ExcedentPNT)})");
        if (r.PNC.Effectif > 0)
            Ligne("  Effectif min PNC",
                $"{r.NMinPNCGroupe} (excedent {SigneVal(r.ExcedentPNCGroupe)})");
        Ligne("  Blocs absorbables", SigneVal(r.BlocsAbsorbables));

        // ── PROGRAMME DÉTAILLÉ ──
        Section("PROGRAMME DETAILLE");
        Console.WriteLine($"  {"Ligne",-12}{"Blocs/mois",12}{"HDV/mois",10}");
        foreach (var (nom, blocs, hdv) in r.DetailProgramme)
            Console.WriteLine($"  {nom,-12}{blocs,12}{hdv,10:F1}");
        TiretLigne();
        Console.WriteLine($"  {"TOTAL",-12}{r.TotalBlocs,12}{r.TotalHDV,10:F1}");

        // ── ALERTES ──
        if (r.Alertes.Count > 0)
        {
            Section("ALERTES");
            foreach (var alerte in r.Alertes)
                Console.WriteLine($"  {alerte}");
        }

        Console.WriteLine();
    }

    private static void AfficherCapaciteGroupe(ResultatGroupe g)
    {
        Section($"CAPACITE {g.Nom} ({g.Effectif} personnes)");
        Ligne("  C_brute", $"{g.CapaciteBrute} jours-pers.");
        Ligne("  Abattements", $"-{g.TotalAbattements}");
        Ligne("  Fonctions sol", $"-{g.TotalJoursSol}");
        Ligne("  C_net", $"{g.CapaciteNette} jours-pers.");
        Ligne("  alpha (disponibilite)", $"{g.Alpha:P1}");
        Ligne("  h_max (mordante)", $"{g.HMax:F1} HDV [{g.ContrainteMordante}]");
        Ligne("  C_net HDV", $"{g.CapaciteNetteHDV:N0} HDV");
    }

    private static void AfficherVerifButees(string label, ResultatGroupe g, LimitesCumulatives lim)
    {
        Console.WriteLine(
            $"{label}  " +
            $"28j: {g.Verif28j.cumul,5:F1}/{lim.H28Max,5:F0} {Coche(g.Verif28j.ok)}   " +
            $"90j: {g.Verif90j.cumul,5:F1}/{lim.H90Max,5:F0} {Coche(g.Verif90j.ok)}   " +
            $"12m: {g.Verif12m.cumul,5:F1}/{lim.H12Max,5:F0} {Coche(g.Verif12m.ok)}");
    }

    private static void AfficherLigneCategorie(ResultatCategorie c)
    {
        string statut = c.Statut switch
        {
            "CONFORTABLE" => "[OK] CONF.",
            "TENDU" => "[!!] TENDU",
            "CRITIQUE" => "[XX] CRIT.",
            _ => c.Statut
        };
        string marge = $"{(c.Marge >= 0 ? "+" : "")}{c.Marge:F1}";
        Console.WriteLine(
            $"  {c.Nom,-6}{c.Effectif,6}{c.Capacite,10:F1}{c.Besoin,8}{marge,9}{c.TauxEngagement,8:P1}  {statut}");
    }

    private static void Section(string titre)
    {
        Console.WriteLine();
        Console.WriteLine($"  {titre}");
        TiretLigne();
    }

    private static void Ligne(string label, string valeur)
    {
        int dots = W - 4 - label.Length - valeur.Length;
        if (dots < 2) dots = 2;
        Console.WriteLine($"{label} {new string('.', dots)} {valeur}");
    }

    private static void DoubleLigne() => Console.WriteLine($"  {new string('=', W - 4)}");
    private static void TiretLigne() => Console.WriteLine($"  {new string('-', W - 4)}");

    private static void Centre(string texte)
    {
        int pad = Math.Max(2, (W - texte.Length) / 2);
        Console.WriteLine($"{new string(' ', pad)}{texte}");
    }

    private static string Coche(bool ok) => ok ? "[OK]" : "[XX]";
    private static string IconeStatut(string s) => s switch
    {
        "CONFORTABLE" => "[OK]",
        "TENDU" => "[!!]",
        "CRITIQUE" => "[XX]",
        _ => "[??]"
    };

    private static string SigneVal(int val) => val >= 0 ? $"+{val}" : $"{val}";
    private static string SigneVal(double val) => val >= 0 ? $"+{val:F1}" : $"{val:F1}";
}
