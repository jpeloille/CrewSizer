using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using CrewSizer.Models;
using CrewSizer.Services;

namespace CrewSizer.IO;

public enum ConfigFileType { Unknown, Legacy, Parametres, Programme, CatalogueVols, Equipage }

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

        // Parametres (Periode, Effectif, Limites, etc.)
        ParseParametresInto(root, config);

        // Detect format: new has <CatalogueBlocs>, old doesn't
        if (root.Element("CatalogueBlocs") != null)
        {
            config.CatalogueVols = ParseCatalogueVolsV2(root.Element("CatalogueVols"));
            config.CatalogueBlocs = ParseCatalogueBlocs(root.Element("CatalogueBlocs"));
            config.SemainesTypes = ParseSemainesTypesV2(root.Element("SemainesTypes"));
            config.Calendrier = ParseCalendrierV2(root.Element("Calendrier"));
        }
        else
        {
            config.CatalogueVols = ParseCatalogueVolsLegacy(root.Element("CatalogueVols"));
            config.SemainesTypes = ParseSemainesTypesLegacy(root.Element("SemainesTypes"));
            config.Calendrier = ParseCalendrierLegacy(root.Element("Calendrier"));
            MigrerVersNouveauFormat(config);
        }

        CatalogueResolver.ResoudreTout(config);
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
                BuildCatalogueBlocs(config.CatalogueBlocs),
                BuildSemainesTypes(config.SemainesTypes),
                BuildCalendrier(config.Calendrier),
                BuildTableTsvMax(config.TableTsvMax),
                new XElement("LimitesTempsService",
                    new XAttribute("max7j", config.LimitesTempsService.Max7j.ToString(Inv)),
                    new XAttribute("max14j", config.LimitesTempsService.Max14j.ToString(Inv)),
                    new XAttribute("max28j", config.LimitesTempsService.Max28j.ToString(Inv)))));

        doc.Save(chemin);
    }

    // ── Detection du type ──

    public static ConfigFileType DetecterType(string chemin)
    {
        if (!File.Exists(chemin)) return ConfigFileType.Unknown;
        try
        {
            using var reader = XmlReader.Create(chemin);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    return reader.LocalName switch
                    {
                        "Configuration" => ConfigFileType.Legacy,
                        "Parametres" => ConfigFileType.Parametres,
                        "Programme" => ConfigFileType.Programme,
                        "CatalogueVols" => ConfigFileType.CatalogueVols,
                        "Equipage" => ConfigFileType.Equipage,
                        _ => ConfigFileType.Unknown,
                    };
                }
            }
        }
        catch { /* malformed XML */ }
        return ConfigFileType.Unknown;
    }

    // ── Chargement par type ──

    public static Configuration ChargerParametres(string chemin)
    {
        var doc = XDocument.Load(chemin);
        var root = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var config = new Configuration();
        ParseParametresInto(root, config);
        return config;
    }

    public static Configuration ChargerProgramme(string chemin)
    {
        var doc = XDocument.Load(chemin);
        var root = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var config = new Configuration();

        var per = root.Element("Periode");
        if (per != null)
        {
            config.Periode.Mois = Attr(per, "mois", "");
            config.Periode.Annee = IntAttr(per, "annee");
            config.Periode.NbJours = IntAttr(per, "nbJours");
        }

        if (root.Element("CatalogueBlocs") != null)
        {
            config.CatalogueVols = ParseCatalogueVolsV2(root.Element("CatalogueVols"));
            config.CatalogueBlocs = ParseCatalogueBlocs(root.Element("CatalogueBlocs"));
            config.SemainesTypes = ParseSemainesTypesV2(root.Element("SemainesTypes"));
            config.Calendrier = ParseCalendrierV2(root.Element("Calendrier"));
        }
        else
        {
            config.SemainesTypes = ParseSemainesTypesLegacy(root.Element("SemainesTypes"));
            config.Calendrier = ParseCalendrierLegacy(root.Element("Calendrier"));
            MigrerVersNouveauFormat(config);
        }

        CatalogueResolver.ResoudreTout(config);
        return config;
    }

    public static Configuration ChargerCatalogueVols(string chemin)
    {
        var doc = XDocument.Load(chemin);
        var root = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var config = new Configuration();
        // Root IS <CatalogueVols>, so parse its children directly
        // Try V2 first (has id attribute), fallback to legacy
        var firstVol = root.Elements("Vol").FirstOrDefault();
        if (firstVol?.Attribute("id") != null)
            config.CatalogueVols = ParseCatalogueVolsV2(root);
        else
            config.CatalogueVols = ParseCatalogueVolsLegacy(root);
        return config;
    }

    // ── Merge partiel ──

    public static void MergerParametres(Configuration cible, Configuration source)
    {
        cible.Effectif = source.Effectif;
        cible.LimitesFTL = source.LimitesFTL;
        cible.LimitesCumulatives = source.LimitesCumulatives;
        cible.JoursOff = source.JoursOff;
        cible.FonctionsSolPNT = source.FonctionsSolPNT;
        cible.FonctionsSolPNC = source.FonctionsSolPNC;
        cible.AbattementsPNT = source.AbattementsPNT;
        cible.AbattementsPNC = source.AbattementsPNC;
        cible.TableTsvMax = source.TableTsvMax;
        cible.LimitesTempsService = source.LimitesTempsService;
    }

    public static void MergerProgramme(Configuration cible, Configuration source)
    {
        cible.Periode = source.Periode;
        cible.CatalogueVols = source.CatalogueVols;
        cible.CatalogueBlocs = source.CatalogueBlocs;
        cible.SemainesTypes = source.SemainesTypes;
        cible.Calendrier = source.Calendrier;
    }

    public static void MergerCatalogueVols(Configuration cible, Configuration source)
    {
        cible.CatalogueVols = source.CatalogueVols;
    }

    // ── Sauvegarde par type ──

    public static void SauvegarderParametres(string chemin, Configuration config)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Parametres",
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
                BuildTableTsvMax(config.TableTsvMax),
                new XElement("LimitesTempsService",
                    new XAttribute("max7j", config.LimitesTempsService.Max7j.ToString(Inv)),
                    new XAttribute("max14j", config.LimitesTempsService.Max14j.ToString(Inv)),
                    new XAttribute("max28j", config.LimitesTempsService.Max28j.ToString(Inv)))));
        doc.Save(chemin);
    }

    public static void SauvegarderProgramme(string chemin, Configuration config)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Programme",
                new XElement("Periode",
                    new XAttribute("mois", config.Periode.Mois),
                    new XAttribute("annee", config.Periode.Annee),
                    new XAttribute("nbJours", config.Periode.NbJours)),
                BuildCatalogueVols(config.CatalogueVols),
                BuildCatalogueBlocs(config.CatalogueBlocs),
                BuildSemainesTypes(config.SemainesTypes),
                BuildCalendrier(config.Calendrier)));
        doc.Save(chemin);
    }

    public static void SauvegarderCatalogueVols(string chemin, Configuration config)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            BuildCatalogueVols(config.CatalogueVols));
        doc.Save(chemin);
    }

    // ── Equipage ──

    public static void MergerEquipage(Configuration cible, DonneesEquipage source)
    {
        cible.Equipage = source;
    }

    public static void SauvegarderEquipage(string chemin, DonneesEquipage equipage)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Equipage",
                new XAttribute("dateExtraction", equipage.DateExtraction.ToString("yyyy-MM-dd")),
                new XElement("Checks",
                    equipage.Checks.Select(c => new XElement("Check",
                        new XAttribute("id", c.Id),
                        new XAttribute("code", c.Code),
                        new XAttribute("description", c.Description),
                        new XAttribute("primaire", c.Primaire),
                        new XAttribute("groupe", c.Groupe),
                        new XAttribute("validite", c.ValiditeNombre),
                        new XAttribute("validiteUnite", c.ValiditeUnite),
                        new XAttribute("finMois", c.FinDeMois),
                        new XAttribute("finAnnee", c.FinDAnnee),
                        new XAttribute("renouvellement", c.RenouvellementNombre),
                        new XAttribute("renouvellementUnite", c.RenouvellementUnite),
                        new XAttribute("avertissement", c.AvertissementNombre),
                        new XAttribute("avertissementUnite", c.AvertissementUnite)))),
                new XElement("Membres",
                    equipage.Membres.Select(m => new XElement("Membre",
                        new XAttribute("id", m.Id),
                        new XAttribute("code", m.Code),
                        new XAttribute("nom", m.Nom),
                        new XAttribute("actif", m.Actif),
                        new XAttribute("contrat", m.Contrat),
                        new XAttribute("grade", m.Grade),
                        new XAttribute("matricule", m.Matricule),
                        new XAttribute("dateEntree", m.DateEntree?.ToString("yyyy-MM-dd") ?? ""),
                        new XAttribute("dateFin", m.DateFin?.ToString("yyyy-MM-dd") ?? ""),
                        new XAttribute("roles", string.Join("+", m.Roles)),
                        new XAttribute("categorie", m.Categorie),
                        new XAttribute("regles", string.Join("+", m.ReglesApplicables)),
                        new XAttribute("bases", string.Join("+", m.Bases)),
                        new XAttribute("typeAvion", m.TypeAvion),
                        m.Qualifications.Select(q => new XElement("Qualification",
                            new XAttribute("check", q.CodeCheck),
                            new XAttribute("expiration", q.DateExpiration?.ToString("yyyy-MM-dd") ?? ""),
                            new XAttribute("statut", q.Statut)))))),
                new XElement("Competences",
                    equipage.Competences.Select(c => new XElement("Competence",
                        new XAttribute("id", c.Id),
                        new XAttribute("code", c.Code),
                        new XAttribute("libelle", c.Libelle),
                        new XAttribute("type", c.Type),
                        new XAttribute("groupe", c.Groupe),
                        c.ChecksRequis.Select(cr => new XElement("CheckRequis",
                            new XAttribute("code", cr)))))),
                new XElement("Affectations",
                    equipage.Affectations.Select(a => new XElement("Affectation",
                        new XAttribute("id", a.Id),
                        new XAttribute("membre", a.MembreId),
                        new XAttribute("date", a.Date.ToString("yyyy-MM-dd")),
                        new XAttribute("activite", a.Activite),
                        new XAttribute("bloc", a.BlocId),
                        new XAttribute("blocCode", a.BlocCode),
                        new XAttribute("commentaire", a.Commentaire),
                        new XAttribute("heuresVol", a.HeuresVol.ToString(Inv)),
                        new XAttribute("tempsService", a.TempsService.ToString(Inv))))),
                new XElement("Indisponibilites",
                    equipage.Indisponibilites.Select(d => new XElement("Indisponibilite",
                        new XAttribute("id", d.Id),
                        new XAttribute("membre", d.MembreId),
                        new XAttribute("membreCode", d.MembreCode),
                        new XAttribute("motif", d.Motif),
                        new XAttribute("dateDebut", d.DateDebut.ToString("yyyy-MM-dd")),
                        new XAttribute("dateFin", d.DateFin?.ToString("yyyy-MM-dd") ?? ""),
                        new XAttribute("commentaire", d.Commentaire)))),
                new XElement("HistoriquesHDV",
                    equipage.HistoriquesHDV.Select(h => new XElement("HistoriqueHDV",
                        new XAttribute("id", h.Id),
                        new XAttribute("membre", h.MembreId),
                        new XAttribute("membreCode", h.MembreCode),
                        new XAttribute("dateReleve", h.DateReleve.ToString("yyyy-MM-dd")),
                        new XAttribute("cumul28j", h.Cumul28j.ToString(Inv)),
                        new XAttribute("cumul90j", h.Cumul90j.ToString(Inv)),
                        new XAttribute("cumul12m", h.Cumul12m.ToString(Inv)))))));
        doc.Save(chemin);
    }

    public static DonneesEquipage ChargerEquipage(string chemin)
    {
        var doc = XDocument.Load(chemin);
        var root = doc.Root ?? throw new InvalidOperationException("Document XML vide");

        var equipage = new DonneesEquipage();

        var dateStr = root.Attribute("dateExtraction")?.Value;
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var de))
            equipage.DateExtraction = de;

        var checksEl = root.Element("Checks");
        if (checksEl != null)
        {
            foreach (var el in checksEl.Elements("Check"))
            {
                equipage.Checks.Add(new DefinitionCheck
                {
                    Id = GuidAttr(el, "id"),
                    Code = Attr(el, "code", ""),
                    Description = Attr(el, "description", ""),
                    Primaire = BoolAttr(el, "primaire"),
                    Groupe = Attr(el, "groupe", "") == "Cockpit" ? GroupeCheck.Cockpit : GroupeCheck.Cabine,
                    ValiditeNombre = IntAttr(el, "validite"),
                    ValiditeUnite = Attr(el, "validiteUnite", ""),
                    FinDeMois = BoolAttr(el, "finMois"),
                    FinDAnnee = BoolAttr(el, "finAnnee"),
                    RenouvellementNombre = IntAttr(el, "renouvellement"),
                    RenouvellementUnite = Attr(el, "renouvellementUnite", ""),
                    AvertissementNombre = IntAttr(el, "avertissement"),
                    AvertissementUnite = Attr(el, "avertissementUnite", "")
                });
            }
        }

        var membresEl = root.Element("Membres");
        if (membresEl != null)
        {
            foreach (var el in membresEl.Elements("Membre"))
            {
                var membre = new MembreEquipage
                {
                    Id = GuidAttr(el, "id"),
                    Code = Attr(el, "code", ""),
                    Nom = Attr(el, "nom", ""),
                    Actif = BoolAttr(el, "actif"),
                    Contrat = Enum.TryParse<TypeContrat>(Attr(el, "contrat", "PNT"), out var tc) ? tc : TypeContrat.PNT,
                    Grade = Enum.TryParse<Grade>(Attr(el, "grade", "OPL"), out var gr) ? gr : Grade.OPL,
                    Matricule = Attr(el, "matricule", ""),
                    DateEntree = ParseDate(Attr(el, "dateEntree", "")),
                    DateFin = ParseDate(Attr(el, "dateFin", "")),
                    Roles = Attr(el, "roles", "").Split('+', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Categorie = Attr(el, "categorie", ""),
                    ReglesApplicables = Attr(el, "regles", "").Split('+', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Bases = Attr(el, "bases", "").Split('+', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    TypeAvion = Attr(el, "typeAvion", "")
                };

                foreach (var qEl in el.Elements("Qualification"))
                {
                    membre.Qualifications.Add(new StatutQualification
                    {
                        CodeCheck = Attr(qEl, "check", ""),
                        DateExpiration = ParseDate(Attr(qEl, "expiration", "")),
                        Statut = Enum.TryParse<StatutCheck>(Attr(qEl, "statut", "NonApplicable"), out var st)
                            ? st : StatutCheck.NonApplicable
                    });
                }

                equipage.Membres.Add(membre);
            }
        }

        var competencesEl = root.Element("Competences");
        if (competencesEl != null)
        {
            foreach (var el in competencesEl.Elements("Competence"))
            {
                var comp = new Competence
                {
                    Id = GuidAttr(el, "id"),
                    Code = Attr(el, "code", ""),
                    Libelle = Attr(el, "libelle", ""),
                    Type = Enum.TryParse<TypeCompetence>(Attr(el, "type", "Qualification"), out var tc2)
                        ? tc2 : TypeCompetence.Qualification,
                    Groupe = Attr(el, "groupe", "") == "Cockpit" ? GroupeCheck.Cockpit : GroupeCheck.Cabine
                };
                foreach (var crEl in el.Elements("CheckRequis"))
                    comp.ChecksRequis.Add(Attr(crEl, "code", ""));
                equipage.Competences.Add(comp);
            }
        }

        var affectationsEl = root.Element("Affectations");
        if (affectationsEl != null)
        {
            foreach (var el in affectationsEl.Elements("Affectation"))
            {
                equipage.Affectations.Add(new AffectationEquipage
                {
                    Id = GuidAttr(el, "id"),
                    MembreId = GuidAttr(el, "membre"),
                    Date = ParseDate(Attr(el, "date", "")) ?? DateTime.MinValue,
                    Activite = Enum.TryParse<TypeActivite>(Attr(el, "activite", "Vol"), out var ta)
                        ? ta : TypeActivite.Vol,
                    BlocId = GuidAttr(el, "bloc"),
                    BlocCode = Attr(el, "blocCode", ""),
                    Commentaire = Attr(el, "commentaire", ""),
                    HeuresVol = DblAttr(el, "heuresVol"),
                    TempsService = DblAttr(el, "tempsService")
                });
            }
        }

        var indispoEl = root.Element("Indisponibilites");
        if (indispoEl != null)
        {
            foreach (var el in indispoEl.Elements("Indisponibilite"))
            {
                equipage.Indisponibilites.Add(new DisponibiliteMembre
                {
                    Id = GuidAttr(el, "id"),
                    MembreId = GuidAttr(el, "membre"),
                    MembreCode = Attr(el, "membreCode", ""),
                    Motif = Enum.TryParse<MotifIndisponibilite>(Attr(el, "motif", "Autre"), out var mi)
                        ? mi : MotifIndisponibilite.Autre,
                    DateDebut = ParseDate(Attr(el, "dateDebut", "")) ?? DateTime.MinValue,
                    DateFin = ParseDate(Attr(el, "dateFin", "")),
                    Commentaire = Attr(el, "commentaire", "")
                });
            }
        }

        var hdvEl = root.Element("HistoriquesHDV");
        if (hdvEl != null)
        {
            foreach (var el in hdvEl.Elements("HistoriqueHDV"))
            {
                equipage.HistoriquesHDV.Add(new HistoriqueHDV
                {
                    Id = GuidAttr(el, "id"),
                    MembreId = GuidAttr(el, "membre"),
                    MembreCode = Attr(el, "membreCode", ""),
                    DateReleve = ParseDate(Attr(el, "dateReleve", "")) ?? DateTime.MinValue,
                    Cumul28j = DblAttr(el, "cumul28j"),
                    Cumul90j = DblAttr(el, "cumul90j"),
                    Cumul12m = DblAttr(el, "cumul12m")
                });
            }
        }

        return equipage;
    }

    // ── Helper parsing parametres ──

    private static void ParseParametresInto(XElement root, Configuration config)
    {
        var per = root.Element("Periode");
        if (per != null)
        {
            config.Periode.Mois = Attr(per, "mois", "");
            config.Periode.Annee = IntAttr(per, "annee");
            config.Periode.NbJours = IntAttr(per, "nbJours");
        }

        var eff = root.Element("Effectif");
        if (eff != null)
        {
            config.Effectif.Cdb = IntAttr(eff, "cdb");
            config.Effectif.Opl = IntAttr(eff, "opl");
            config.Effectif.Cc = IntAttr(eff, "cc");
            config.Effectif.Pnc = IntAttr(eff, "pnc");
        }

        var ftl = root.Element("LimitesFTL");
        if (ftl != null)
        {
            config.LimitesFTL.TsvMaxJournalier = DblAttr(ftl, "tsvMaxJournalier", 13.0);
            config.LimitesFTL.TsvMoyenRetenu = DblAttr(ftl, "tsvMoyenRetenu", 10.0);
            config.LimitesFTL.ReposMinimum = DblAttr(ftl, "reposMinimum", 12.0);
        }

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

        var off = root.Element("JoursOff");
        if (off != null)
        {
            config.JoursOff.Reglementaire = IntAttr(off, "reglementaire");
            config.JoursOff.AccordEntreprise = IntAttr(off, "accordEntreprise");
        }

        config.FonctionsSolPNT = ParseFonctionsSol(root.Element("FonctionsSolPNT"));
        config.FonctionsSolPNC = ParseFonctionsSol(root.Element("FonctionsSolPNC"));
        config.AbattementsPNT = ParseAbattements(root.Element("AbattementsPNT"));
        config.AbattementsPNC = ParseAbattements(root.Element("AbattementsPNC"));
        config.TableTsvMax = ParseTableTsvMax(root.Element("TableTsvMax"));

        var lts = root.Element("LimitesTempsService");
        if (lts != null)
        {
            config.LimitesTempsService.Max7j = DblAttr(lts, "max7j", 60);
            config.LimitesTempsService.Max14j = DblAttr(lts, "max14j", 110);
            config.LimitesTempsService.Max28j = DblAttr(lts, "max28j", 190);
        }
    }

    // ── Helpers lecture ──

    private static string Attr(XElement el, string name, string def = "") =>
        el.Attribute(name)?.Value ?? def;

    private static int IntAttr(XElement el, string name, int def = 0) =>
        int.TryParse(el.Attribute(name)?.Value, NumberStyles.Integer, Inv, out var v) ? v : def;

    private static double DblAttr(XElement el, string name, double def = 0) =>
        double.TryParse(el.Attribute(name)?.Value, NumberStyles.Float, Inv, out var v) ? v : def;

    private static Guid GuidAttr(XElement el, string name) =>
        Guid.TryParse(el.Attribute(name)?.Value, out var v) ? v : Guid.Empty;

    private static bool BoolAttr(XElement el, string name) =>
        bool.TryParse(el.Attribute(name)?.Value, out var v) && v;

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

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

    // ── Parsing V2 (nouveau format avec UUID) ──

    private static List<Vol> ParseCatalogueVolsV2(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Vol").Select(v => new Vol
        {
            Id = GuidAttr(v, "id"),
            Numero = Attr(v, "numero"),
            Depart = Attr(v, "depart"),
            Arrivee = Attr(v, "arrivee"),
            HeureDepart = Attr(v, "heureDepart"),
            HeureArrivee = Attr(v, "heureArrivee"),
            MH = Attr(v, "mh") == "true"
        }).ToList();
    }

    private static List<BlocVol> ParseCatalogueBlocs(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Bloc").Select(e => new BlocVol
        {
            Id = GuidAttr(e, "id"),
            Code = Attr(e, "code"),
            Periode = Attr(e, "periode"),
            DebutDP = Attr(e, "debutDP"),
            FinDP = Attr(e, "finDP"),
            DebutFDP = Attr(e, "debutFDP"),
            FinFDP = Attr(e, "finFDP"),
            Etapes = e.Elements("Etape").Select(et => new EtapeVol
            {
                Position = IntAttr(et, "position"),
                VolId = GuidAttr(et, "vol")
            }).ToList()
        }).ToList();
    }

    private static List<SemaineType> ParseSemainesTypesV2(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("SemaineType").Select(st => new SemaineType
        {
            Id = GuidAttr(st, "id"),
            Reference = Attr(st, "reference"),
            Saison = Attr(st, "saison"),
            Placements = st.Elements("Placement").Select(p => new BlocPlacement
            {
                Sequence = IntAttr(p, "sequence"),
                Jour = Attr(p, "jour"),
                BlocId = GuidAttr(p, "bloc")
            }).ToList()
        }).ToList();
    }

    private static List<AffectationSemaine> ParseCalendrierV2(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Affectation").Select(e => new AffectationSemaine
        {
            Semaine = IntAttr(e, "semaine"),
            Annee = IntAttr(e, "annee"),
            SemaineTypeId = GuidAttr(e, "ref")
        }).ToList();
    }

    // ── Parsing Legacy (ancien format imbriqué) ──

    private static List<Vol> ParseCatalogueVolsLegacy(XElement? parent)
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

    private static List<SemaineType> ParseSemainesTypesLegacy(XElement? parent)
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

    private static List<AffectationSemaine> ParseCalendrierLegacy(XElement? parent)
    {
        if (parent == null) return [];
        return parent.Elements("Affectation").Select(e => new AffectationSemaine
        {
            Semaine = IntAttr(e, "semaine"),
            Annee = IntAttr(e, "annee"),
            SemaineTypeRef = Attr(e, "ref")
        }).ToList();
    }

    // ── Migration ancien format → nouveau ──

    private static void MigrerVersNouveauFormat(Configuration config)
    {
        // 1. Extraire les vols uniques depuis tous les blocs imbriqués
        var volsMap = new Dictionary<string, Vol>();
        foreach (var st in config.SemainesTypes)
        {
            foreach (var bloc in st.Blocs)
            {
                foreach (var vol in bloc.Vols)
                {
                    var sig = VolSignature(vol);
                    if (!volsMap.ContainsKey(sig))
                    {
                        vol.Id = Guid.NewGuid();
                        volsMap[sig] = vol;
                    }
                }
            }
        }

        // Merger avec le CatalogueVols existant (s'il y en a)
        foreach (var catVol in config.CatalogueVols)
        {
            var sig = VolSignature(catVol);
            if (!volsMap.ContainsKey(sig))
            {
                catVol.Id = Guid.NewGuid();
                volsMap[sig] = catVol;
            }
        }

        config.CatalogueVols = volsMap.Values.ToList();

        // 2. Extraire les blocs uniques (par signature DP/FDP + vols)
        var blocsMap = new Dictionary<string, BlocVol>();
        var codeCounter = new Dictionary<string, int>();

        foreach (var st in config.SemainesTypes)
        {
            foreach (var bloc in st.Blocs)
            {
                var etapes = bloc.Vols.Select((v, i) => new EtapeVol
                {
                    Position = i + 1,
                    VolId = volsMap[VolSignature(v)].Id
                }).ToList();

                var volSigs = string.Join(";", etapes.Select(e => e.VolId));
                var blocSig = $"{bloc.Periode}|{bloc.DebutDP}|{bloc.FinDP}|{bloc.DebutFDP}|{bloc.FinFDP}|{volSigs}";

                if (!blocsMap.ContainsKey(blocSig))
                {
                    bloc.Id = Guid.NewGuid();
                    bloc.Etapes = etapes;
                    bloc.Code = GenererCodeBloc(bloc, codeCounter);
                    blocsMap[blocSig] = bloc;
                }
            }
        }

        config.CatalogueBlocs = blocsMap.Values.ToList();

        // 3. Convertir SemaineType.Blocs → Placements
        foreach (var st in config.SemainesTypes)
        {
            st.Id = Guid.NewGuid();
            st.Placements.Clear();

            foreach (var bloc in st.Blocs)
            {
                var etapes = bloc.Vols.Select((v, i) => new EtapeVol
                {
                    Position = i + 1,
                    VolId = volsMap[VolSignature(v)].Id
                }).ToList();

                var volSigs = string.Join(";", etapes.Select(e => e.VolId));
                var blocSig = $"{bloc.Periode}|{bloc.DebutDP}|{bloc.FinDP}|{bloc.DebutFDP}|{bloc.FinFDP}|{volSigs}";

                st.Placements.Add(new BlocPlacement
                {
                    BlocId = blocsMap[blocSig].Id,
                    Jour = bloc.Jour,
                    Sequence = bloc.Sequence
                });
            }
        }

        // 4. Fixer le calendrier (SemaineTypeRef string → SemaineTypeId Guid)
        var stDict = config.SemainesTypes.ToDictionary(s => s.Reference);
        foreach (var aff in config.Calendrier)
        {
            if (stDict.TryGetValue(aff.SemaineTypeRef, out var stFound))
                aff.SemaineTypeId = stFound.Id;
        }
    }

    private static string VolSignature(Vol v) =>
        $"{v.Numero}|{v.Depart}|{v.Arrivee}|{v.HeureDepart}|{v.HeureArrivee}|{v.MH}";

    private static string GenererCodeBloc(BlocVol bloc, Dictionary<string, int> counter)
    {
        var dest = bloc.Vols.Count > 0 ? bloc.Vols[0].Arrivee : "???";
        var baseCode = $"ROT-{dest}-{bloc.Periode}";

        if (!counter.TryGetValue(baseCode, out var count))
        {
            counter[baseCode] = 1;
            return baseCode;
        }

        counter[baseCode] = count + 1;
        return $"{baseCode}-{count + 1}";
    }

    // ── Helpers Table TSV Max ──

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

    // ── Helpers écriture (nouveau format V2) ──

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
        new("CatalogueVols", list.Select(v =>
        {
            var el = new XElement("Vol",
                new XAttribute("id", v.Id),
                new XAttribute("numero", v.Numero),
                new XAttribute("depart", v.Depart),
                new XAttribute("arrivee", v.Arrivee),
                new XAttribute("heureDepart", v.HeureDepart),
                new XAttribute("heureArrivee", v.HeureArrivee));
            if (v.MH) el.Add(new XAttribute("mh", "true"));
            return el;
        }));

    private static XElement BuildCatalogueBlocs(List<BlocVol> list) =>
        new("CatalogueBlocs", list.Select(b => new XElement("Bloc",
            new XAttribute("id", b.Id),
            new XAttribute("code", b.Code),
            new XAttribute("periode", b.Periode),
            new XAttribute("debutDP", b.DebutDP),
            new XAttribute("finDP", b.FinDP),
            new XAttribute("debutFDP", b.DebutFDP),
            new XAttribute("finFDP", b.FinFDP),
            b.Etapes.OrderBy(e => e.Position).Select(e => new XElement("Etape",
                new XAttribute("position", e.Position),
                new XAttribute("vol", e.VolId))))));

    private static XElement BuildSemainesTypes(List<SemaineType> list) =>
        new("SemainesTypes", list.Select(st => new XElement("SemaineType",
            new XAttribute("id", st.Id),
            new XAttribute("reference", st.Reference),
            new XAttribute("saison", st.Saison),
            st.Placements.Select(p => new XElement("Placement",
                new XAttribute("sequence", p.Sequence),
                new XAttribute("jour", p.Jour),
                new XAttribute("bloc", p.BlocId))))));

    private static XElement BuildCalendrier(List<AffectationSemaine> list) =>
        new("Calendrier", list.Select(a => new XElement("Affectation",
            new XAttribute("semaine", a.Semaine),
            new XAttribute("annee", a.Annee),
            new XAttribute("ref", a.SemaineTypeId))));

    private static XElement BuildTableTsvMax(List<EntreeTsvMax> list) =>
        new("TableTsvMax", list.Select(e => new XElement("Bande",
            new XAttribute("debut", e.DebutBande),
            new XAttribute("fin", e.FinBande),
            e.MaxParEtapes.OrderBy(kv => kv.Key).Select(kv => new XElement("Max",
                new XAttribute("etapes", kv.Key),
                new XAttribute("heures", kv.Value.ToString(Inv)))))));
}
