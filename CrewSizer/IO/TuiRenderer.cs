using CrewSizer.Models;
using CrewSizer.Tui;

namespace CrewSizer.IO;

public static class TuiRenderer
{
    private const int W = 66;

    public static void Afficher(ResultatMarge r, Configuration config, IOutputWriter o, ITheme t)
    {
        var ok = t.AlertOk;
        var warn = t.AlertWarning;
        var crit = t.AlertExceeded;
        var rst = t.Reset;

        o.WriteLine();
        DblLine(o);
        Centre(o, $"MARGE D'ENGAGEMENT EQUIPAGE -- {r.Mois} {r.Annee} ({r.NbJours} jours)");
        DblLine(o);

        // EFFECTIF
        Sect(o, "EFFECTIF");
        o.WriteLine("  PNT");
        Kv(o, "    CDB", r.CDB.Effectif.ToString());
        Kv(o, "    OPL", r.OPL.Effectif.ToString());
        Kv(o, "    Total PNT", $"{r.PNT.Effectif} pilotes");
        o.WriteLine("  PNC");
        Kv(o, "    CC", r.CC.Effectif.ToString());
        Kv(o, "    PNC", r.PNCDetail.Effectif.ToString());
        Kv(o, "    Total PNC", $"{r.PNC.Effectif}");
        Kv(o, "  Equipage / vol", "4  (1 CDB + 1 OPL + 1 CC + 1 PNC/CC)");

        // DISPONIBILITE
        Sect(o, "DISPONIBILITE");
        Kv(o, "  D_dispo (j engageables)", $"{r.DDisponible} j/personne");
        Kv(o, "  Cycle TSV+repos", $"{r.Cycle:F1} h");

        // CAPACITE PNT
        CapGroupe(o, r.PNT);

        // CAPACITE PNC
        if (r.PNC.Effectif > 0)
            CapGroupe(o, r.PNC);

        // VERIFICATIONS CUMULATIVES
        var lim = config.LimitesCumulatives;
        Sect(o, "VERIFICATIONS CUMULATIVES");
        VerifLine(o, "  PNT", r.PNT, lim, t);
        if (r.PNC.Effectif > 0)
            VerifLine(o, "  PNC", r.PNC, lim, t);

        // BESOIN OPERATIONNEL
        Sect(o, "BESOIN OPERATIONNEL");
        Kv(o, "  Blocs / mois", r.TotalBlocs.ToString());
        Kv(o, "  HDV / mois", $"{r.TotalHDV:F1} HDV");
        Kv(o, "  Rotations", r.Rotations.ToString());
        Kv(o, "  s (etapes/rotation)", $"{r.EtapesParRotation:F0}");
        Kv(o, "  Composition cabine",
            $"{r.RotationsAvecPNC} x (CC+PNC) | {r.RotationsSansPNC} x (2xCC)");

        // MARGE PAR CATEGORIE
        o.WriteLine();
        o.WriteLine("  MARGE PAR CATEGORIE");
        DblLine(o);
        o.WriteLine($"  {"Cat.",-6}{"Eff.",6}{"Capacite",10}{"Besoin",8}{"Marge",9}{"Tau",9}  Statut");
        CatLine(o, r.CDB, t);
        CatLine(o, r.OPL, t);
        CatLine(o, r.CC, t);
        if (r.PNCDetail.Effectif > 0)
            CatLine(o, r.PNCDetail, t);
        DblLine(o);

        // RESULTAT GLOBAL
        o.WriteLine();
        o.WriteLine("  RESULTAT GLOBAL");
        DblLine(o);
        string statusColor = r.StatutGlobal switch
        {
            "CONFORTABLE" => ok,
            "TENDU" => warn,
            _ => crit
        };
        string icone = r.StatutGlobal switch
        {
            "CONFORTABLE" => "[OK]",
            "TENDU" => "[!!]",
            _ => "[XX]"
        };
        o.WriteLine($"  | CATEGORIE CONTRAIGNANTE ... {r.CategorieContraignante,-30}|");
        o.WriteLine($"  | TAUX D'ENGAGEMENT ........ {r.TauxEngagementGlobal,6:P1}{"",24}|");
        o.WriteLine($"  | STATUT ................... {statusColor}{icone} {r.StatutGlobal}{rst,-25}|");
        DblLine(o);

        // ANALYSES
        Sect(o, "ANALYSES");
        Kv(o, "  Effectif min PNT", $"{r.NMinPNT} pilotes (excedent {Sv(r.ExcedentPNT)})");
        if (r.PNC.Effectif > 0)
            Kv(o, "  Effectif min PNC", $"{r.NMinPNCGroupe} (excedent {Sv(r.ExcedentPNCGroupe)})");
        Kv(o, "  Blocs absorbables", Sv(r.BlocsAbsorbables));

        // PROGRAMME DETAILLE
        Sect(o, "PROGRAMME DETAILLE");
        o.WriteLine($"  {"Ligne",-12}{"Blocs/mois",12}{"HDV/mois",10}");
        foreach (var (nom, blocs, hdv) in r.DetailProgramme)
            o.WriteLine($"  {nom,-12}{blocs,12}{hdv,10:F1}");
        Tiret(o);
        o.WriteLine($"  {"TOTAL",-12}{r.TotalBlocs,12}{r.TotalHDV,10:F1}");

        // ALERTES
        if (r.Alertes.Count > 0)
        {
            Sect(o, "ALERTES");
            foreach (var a in r.Alertes)
                o.WriteLine($"  {crit}{a}{rst}");
        }

        o.WriteLine();
    }

    private static void CapGroupe(IOutputWriter o, ResultatGroupe g)
    {
        Sect(o, $"CAPACITE {g.Nom} ({g.Effectif} personnes)");
        Kv(o, "  C_brute", $"{g.CapaciteBrute} jours-pers.");
        Kv(o, "  Abattements", $"-{g.TotalAbattements}");
        Kv(o, "  Fonctions sol", $"-{g.TotalJoursSol}");
        Kv(o, "  C_net", $"{g.CapaciteNette} jours-pers.");
        Kv(o, "  alpha (disponibilite)", $"{g.Alpha:P1}");
        Kv(o, "  h_max (mordante)", $"{g.HMax:F1} HDV [{g.ContrainteMordante}]");
        Kv(o, "  C_net HDV", $"{g.CapaciteNetteHDV:N0} HDV");
    }

    private static void VerifLine(IOutputWriter o, string label, ResultatGroupe g,
        LimitesCumulatives lim, ITheme t)
    {
        string C(bool ok) => ok ? $"{t.AlertOk}[OK]{t.Reset}" : $"{t.AlertExceeded}[XX]{t.Reset}";
        o.WriteLine(
            $"{label}  " +
            $"28j: {g.Verif28j.cumul,5:F1}/{lim.H28Max,5:F0} {C(g.Verif28j.ok)}   " +
            $"90j: {g.Verif90j.cumul,5:F1}/{lim.H90Max,5:F0} {C(g.Verif90j.ok)}   " +
            $"12m: {g.Verif12m.cumul,5:F1}/{lim.H12Max,5:F0} {C(g.Verif12m.ok)}");
    }

    private static void CatLine(IOutputWriter o, ResultatCategorie c, ITheme t)
    {
        string color = c.Statut switch
        {
            "CONFORTABLE" => t.AlertOk,
            "TENDU" => t.AlertWarning,
            _ => t.AlertExceeded
        };
        string statut = c.Statut switch
        {
            "CONFORTABLE" => "[OK] CONF.",
            "TENDU" => "[!!] TENDU",
            "CRITIQUE" => "[XX] CRIT.",
            _ => c.Statut
        };
        string marge = $"{(c.Marge >= 0 ? "+" : "")}{c.Marge:F1}";
        o.WriteLine(
            $"  {c.Nom,-6}{c.Effectif,6}{c.Capacite,10:F1}{c.Besoin,8}{marge,9}{c.TauxEngagement,8:P1}  {color}{statut}{t.Reset}");
    }

    private static void Sect(IOutputWriter o, string titre)
    {
        o.WriteLine();
        o.WriteLine($"  {titre}");
        Tiret(o);
    }

    private static void Kv(IOutputWriter o, string label, string val)
    {
        int dots = W - 4 - label.Length - val.Length;
        if (dots < 2) dots = 2;
        o.WriteLine($"{label} {new string('.', dots)} {val}");
    }

    private static void DblLine(IOutputWriter o) => o.WriteLine($"  {new string('=', W - 4)}");
    private static void Tiret(IOutputWriter o) => o.WriteLine($"  {new string('-', W - 4)}");

    private static void Centre(IOutputWriter o, string texte)
    {
        int pad = Math.Max(2, (W - texte.Length) / 2);
        o.WriteLine($"{new string(' ', pad)}{texte}");
    }

    private static string Sv(int val) => val >= 0 ? $"+{val}" : $"{val}";
}
