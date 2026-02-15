using System.Globalization;
using System.Xml.Linq;
using CrewSizer.Models;

namespace CrewSizer.IO;

public static class XmlConfigLoader
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static Configuration Charger(string chemin)
    {
        if (!File.Exists(chemin))
            throw new FileNotFoundException($"Fichier de configuration introuvable : {chemin}");

        var doc = XDocument.Load(chemin);
        var root = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var config = new Configuration();

        // Periode
        var per = root.Element("Periode");
        if (per != null)
        {
            config.Periode.Mois = Attr(per, "mois", "");
            config.Periode.Annee = IntAttr(per, "annee");
            config.Periode.NbJours = IntAttr(per, "nbJours");
        }

        // Effectif
        var eff = root.Element("Effectif");
        if (eff != null)
        {
            config.Effectif.Cdb = IntAttr(eff, "cdb");
            config.Effectif.Opl = IntAttr(eff, "opl");
            config.Effectif.Cc = IntAttr(eff, "cc");
            config.Effectif.Pnc = IntAttr(eff, "pnc");
        }

        // LimitesFTL
        var ftl = root.Element("LimitesFTL");
        if (ftl != null)
        {
            config.LimitesFTL.TsvMaxJournalier = DblAttr(ftl, "tsvMaxJournalier", 13.0);
            config.LimitesFTL.TsvMoyenRetenu = DblAttr(ftl, "tsvMoyenRetenu", 10.0);
            config.LimitesFTL.ReposMinimum = DblAttr(ftl, "reposMinimum", 12.0);
        }

        // LimitesCumulatives
        var cumul = root.Element("LimitesCumulatives");
        if (cumul != null)
        {
            config.LimitesCumulatives.H28Max = DblAttr(cumul, "h28Max", 100);
            config.LimitesCumulatives.H90Max = DblAttr(cumul, "h90Max", 280);
            config.LimitesCumulatives.H12Max = DblAttr(cumul, "h12Max", 900);

            var cpnt = cumul.Element("CumulPNT");
            if (cpnt != null)
            {
                config.LimitesCumulatives.CumulPNT.Cumul28Entrant = DblAttr(cpnt, "cumul28");
                config.LimitesCumulatives.CumulPNT.Cumul90Entrant = DblAttr(cpnt, "cumul90");
                config.LimitesCumulatives.CumulPNT.Cumul12Entrant = DblAttr(cpnt, "cumul12");
            }

            var cpnc = cumul.Element("CumulPNC");
            if (cpnc != null)
            {
                config.LimitesCumulatives.CumulPNC.Cumul28Entrant = DblAttr(cpnc, "cumul28");
                config.LimitesCumulatives.CumulPNC.Cumul90Entrant = DblAttr(cpnc, "cumul90");
                config.LimitesCumulatives.CumulPNC.Cumul12Entrant = DblAttr(cpnc, "cumul12");
            }
        }

        // JoursOff
        var off = root.Element("JoursOff");
        if (off != null)
        {
            config.JoursOff.Reglementaire = IntAttr(off, "reglementaire");
            config.JoursOff.AccordEntreprise = IntAttr(off, "accordEntreprise");
        }

        // FonctionsSol
        config.FonctionsSolPNT = ParseFonctionsSol(root.Element("FonctionsSolPNT"));
        config.FonctionsSolPNC = ParseFonctionsSol(root.Element("FonctionsSolPNC"));

        // Abattements
        config.AbattementsPNT = ParseAbattements(root.Element("AbattementsPNT"));
        config.AbattementsPNC = ParseAbattements(root.Element("AbattementsPNC"));

        // Catalogue vols
        config.CatalogueVols = ParseCatalogueVols(root.Element("CatalogueVols"));

        // Semaines types
        config.SemainesTypes = ParseSemainesTypes(root.Element("SemainesTypes"));

        // Calendrier
        config.Calendrier = ParseCalendrier(root.Element("Calendrier"));

        // TableTsvMax
        config.TableTsvMax = ParseTableTsvMax(root.Element("TableTsvMax"));

        // LimitesTempsService
        var lts = root.Element("LimitesTempsService");
        if (lts != null)
        {
            config.LimitesTempsService.Max7j = DblAttr(lts, "max7j", 60);
            config.LimitesTempsService.Max14j = DblAttr(lts, "max14j", 110);
            config.LimitesTempsService.Max28j = DblAttr(lts, "max28j", 190);
        }

        ConfigLoader.Valider(config);
        return config;
    }

    public static void Sauvegarder(string chemin, Configuration config)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Configuration",
                new XElement("Periode",
                    new XAttribute("mois", config.Periode.Mois),
                    new XAttribute("annee", config.Periode.Annee),
                    new XAttribute("nbJours", config.Periode.NbJours)),
                new XElement("Effectif",
                    new XAttribute("cdb", config.Effectif.Cdb),
                    new XAttribute("opl", config.Effectif.Opl),
                    new XAttribute("cc", config.Effectif.Cc),
                    new XAttribute("pnc", config.Effectif.Pnc)),
                new XElement("LimitesFTL",
                    new XAttribute("tsvMaxJournalier", config.LimitesFTL.TsvMaxJournalier.ToString(Inv)),
                    new XAttribute("tsvMoyenRetenu", config.LimitesFTL.TsvMoyenRetenu.ToString(Inv)),
                    new XAttribute("reposMinimum", config.LimitesFTL.ReposMinimum.ToString(Inv))),
                new XElement("LimitesCumulatives",
                    new XAttribute("h28Max", config.LimitesCumulatives.H28Max.ToString(Inv)),
                    new XAttribute("h90Max", config.LimitesCumulatives.H90Max.ToString(Inv)),
                    new XAttribute("h12Max", config.LimitesCumulatives.H12Max.ToString(Inv)),
                    new XElement("CumulPNT",
                        new XAttribute("cumul28", config.LimitesCumulatives.CumulPNT.Cumul28Entrant.ToString(Inv)),
                        new XAttribute("cumul90", config.LimitesCumulatives.CumulPNT.Cumul90Entrant.ToString(Inv)),
                        new XAttribute("cumul12", config.LimitesCumulatives.CumulPNT.Cumul12Entrant.ToString(Inv))),
                    new XElement("CumulPNC",
                        new XAttribute("cumul28", config.LimitesCumulatives.CumulPNC.Cumul28Entrant.ToString(Inv)),
                        new XAttribute("cumul90", config.LimitesCumulatives.CumulPNC.Cumul90Entrant.ToString(Inv)),
                        new XAttribute("cumul12", config.LimitesCumulatives.CumulPNC.Cumul12Entrant.ToString(Inv)))),
                new XElement("JoursOff",
                    new XAttribute("reglementaire", config.JoursOff.Reglementaire),
                    new XAttribute("accordEntreprise", config.JoursOff.AccordEntreprise)),
                BuildFonctionsSol("FonctionsSolPNT", config.FonctionsSolPNT),
                BuildFonctionsSol("FonctionsSolPNC", config.FonctionsSolPNC),
                BuildAbattements("AbattementsPNT", config.AbattementsPNT),
                BuildAbattements("AbattementsPNC", config.AbattementsPNC),
                BuildCatalogueVols(config.CatalogueVols),
                BuildSemainesTypes(config.SemainesTypes),
                BuildCalendrier(config.Calendrier),
                BuildTableTsvMax(config.TableTsvMax),
                new XElement("LimitesTempsService",
                    new XAttribute("max7j", config.LimitesTempsService.Max7j.ToString(Inv)),
                    new XAttribute("max14j", config.LimitesTempsService.Max14j.ToString(Inv)),
                    new XAttribute("max28j", config.LimitesTempsService.Max28j.ToString(Inv)))));

        doc.Save(chemin);
    }

    // ── Helpers lecture ──

    private static string Attr(XElement el, string name, string def = "") =>
        el.Attribute(name)?.Value ?? def;

    private static int IntAttr(XElement el, string name, int def = 0) =>
        int.TryParse(el.Attribute(name)?.Value, NumberStyles.Integer, Inv, out var v) ? v : def;

    private static double DblAttr(XElement el, string name, double def = 0) =>
        double.TryParse(el.Attribute(name)?.Value, NumberStyles.Float, Inv, out var v) ? v : def;

    private static List<FonctionSol> ParseFonctionsSol(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Fonction").Select(e => new FonctionSol
        {
            Nom = Attr(e, "nom"),
            NbPersonnes = IntAttr(e, "nbPersonnes"),
            JoursSolMois = IntAttr(e, "joursSolMois")
        }).ToList();
    }

    private static List<Abattement> ParseAbattements(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Abattement").Select(e => new Abattement
        {
            Libelle = Attr(e, "libelle"),
            JoursPersonnel = IntAttr(e, "joursPersonnel")
        }).ToList();
    }

    private static List<SemaineType> ParseSemainesTypes(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("SemaineType").Select(st => new SemaineType
        {
            Reference = Attr(st, "reference"),
            Saison = Attr(st, "saison"),
            Blocs = st.Elements("Bloc").Select(e => new BlocVol
            {
                Sequence = IntAttr(e, "sequence"),
                Jour = Attr(e, "jour"),
                Periode = Attr(e, "periode"),
                DebutDP = Attr(e, "debutDP"),
                FinDP = Attr(e, "finDP"),
                DebutFDP = Attr(e, "debutFDP"),
                FinFDP = Attr(e, "finFDP"),
                Vols = e.Elements("Vol").Select(v => new Vol
                {
                    Numero = Attr(v, "numero"),
                    Depart = Attr(v, "depart"),
                    Arrivee = Attr(v, "arrivee"),
                    HeureDepart = Attr(v, "heureDepart"),
                    HeureArrivee = Attr(v, "heureArrivee"),
                    MH = Attr(v, "mh") == "true"
                }).ToList()
            }).ToList()
        }).ToList();
    }

    private static List<Vol> ParseCatalogueVols(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Vol").Select(v => new Vol
        {
            Numero = Attr(v, "numero"),
            Depart = Attr(v, "depart"),
            Arrivee = Attr(v, "arrivee"),
            HeureDepart = Attr(v, "heureDepart"),
            HeureArrivee = Attr(v, "heureArrivee")
        }).ToList();
    }

    private static List<AffectationSemaine> ParseCalendrier(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Affectation").Select(e => new AffectationSemaine
        {
            Semaine = IntAttr(e, "semaine"),
            Annee = IntAttr(e, "annee"),
            SemaineTypeRef = Attr(e, "ref")
        }).ToList();
    }

    private static List<EntreeTsvMax> ParseTableTsvMax(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Bande").Select(e =>
        {
            var entree = new EntreeTsvMax
            {
                DebutBande = Attr(e, "debut"),
                FinBande = Attr(e, "fin")
            };
            foreach (var m in e.Elements("Max"))
            {
                int etapes = IntAttr(m, "etapes");
                double max = DblAttr(m, "heures");
                if (etapes > 0) entree.MaxParEtapes[etapes] = max;
            }
            return entree;
        }).ToList();
    }

    // ── Helpers ecriture ──

    private static XElement BuildFonctionsSol(string name, List<FonctionSol> list) =>
        new(name, list.Select(f => new XElement("Fonction",
            new XAttribute("nom", f.Nom),
            new XAttribute("nbPersonnes", f.NbPersonnes),
            new XAttribute("joursSolMois", f.JoursSolMois))));

    private static XElement BuildAbattements(string name, List<Abattement> list) =>
        new(name, list.Select(a => new XElement("Abattement",
            new XAttribute("libelle", a.Libelle),
            new XAttribute("joursPersonnel", a.JoursPersonnel))));

    private static XElement BuildCatalogueVols(List<Vol> list) =>
        new("CatalogueVols", list.Select(v => new XElement("Vol",
            new XAttribute("numero", v.Numero),
            new XAttribute("depart", v.Depart),
            new XAttribute("arrivee", v.Arrivee),
            new XAttribute("heureDepart", v.HeureDepart),
            new XAttribute("heureArrivee", v.HeureArrivee))));

    private static XElement BuildSemainesTypes(List<SemaineType> list) =>
        new("SemainesTypes", list.Select(st => new XElement("SemaineType",
            new XAttribute("reference", st.Reference),
            new XAttribute("saison", st.Saison),
            st.Blocs.Select(b => new XElement("Bloc",
                new XAttribute("sequence", b.Sequence),
                new XAttribute("jour", b.Jour),
                new XAttribute("periode", b.Periode),
                new XAttribute("debutDP", b.DebutDP),
                new XAttribute("finDP", b.FinDP),
                new XAttribute("debutFDP", b.DebutFDP),
                new XAttribute("finFDP", b.FinFDP),
                b.Vols.Select(v =>
                {
                    var el = new XElement("Vol",
                        new XAttribute("numero", v.Numero),
                        new XAttribute("depart", v.Depart),
                        new XAttribute("arrivee", v.Arrivee),
                        new XAttribute("heureDepart", v.HeureDepart),
                        new XAttribute("heureArrivee", v.HeureArrivee));
                    if (v.MH) el.Add(new XAttribute("mh", "true"));
                    return el;
                }))))));

    private static XElement BuildCalendrier(List<AffectationSemaine> list) =>
        new("Calendrier", list.Select(a => new XElement("Affectation",
            new XAttribute("semaine", a.Semaine),
            new XAttribute("annee", a.Annee),
            new XAttribute("ref", a.SemaineTypeRef))));

    private static XElement BuildTableTsvMax(List<EntreeTsvMax> list) =>
        new("TableTsvMax", list.Select(e => new XElement("Bande",
            new XAttribute("debut", e.DebutBande),
            new XAttribute("fin", e.FinBande),
            e.MaxParEtapes.OrderBy(kv => kv.Key).Select(kv => new XElement("Max",
                new XAttribute("etapes", kv.Key),
                new XAttribute("heures", kv.Value.ToString(Inv)))))));
}
