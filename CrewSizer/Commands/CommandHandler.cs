using System.Globalization;
using System.Text;
using CrewSizer.Helpers;
using CrewSizer.IO;
using CrewSizer.Models;
using CrewSizer.Services;
using CrewSizer.Tui;

namespace CrewSizer.Commands;

public class CommandHandler
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private Configuration _config;
    private readonly IOutputWriter _output;
    private readonly ITheme _theme;
    private readonly AppSettings _settings;
    private string? _paramPath;
    private string? _progPath;
    private string? _volsPath;
    private string? _equipagePath;
    private bool _dirty;

    public Configuration Config => _config;
    public bool IsDirty => _dirty;
    public string? ParamPath => _paramPath;
    public string? ProgPath => _progPath;
    public string? VolsPath => _volsPath;
    public string? EquipagePath => _equipagePath;
    public AppSettings Settings => _settings;

    public void MarkDirty()
    {
        _dirty = true;
        CatalogueResolver.ResoudreTout(_config);
        AutoSave();
    }

    private void AutoSave()
    {
        var dir = _settings.RepertoireEffectif;
        _paramPath ??= Path.Combine(dir, "Parametres.xml");
        _progPath ??= Path.Combine(dir, "Programme.xml");
        _volsPath ??= Path.Combine(dir, "CatalogueVols.xml");

        XmlConfigLoader.SauvegarderParametres(_paramPath, _config);
        XmlConfigLoader.SauvegarderProgramme(_progPath, _config);
        XmlConfigLoader.SauvegarderCatalogueVols(_volsPath, _config);

        if (_config.Equipage != null)
        {
            _equipagePath ??= Path.Combine(dir, "Equipage.xml");
            XmlConfigLoader.SauvegarderEquipage(_equipagePath, _config.Equipage);
        }

        _dirty = false;

        _settings.DernierParametres = _paramPath;
        _settings.DernierProgramme = _progPath;
        _settings.DernierCatalogueVols = _volsPath;
        if (_equipagePath != null)
            _settings.DernierEquipage = _equipagePath;
        _settings.Sauvegarder();
    }

    public CommandHandler(Configuration config, IOutputWriter output, ITheme theme,
        string? paramPath, string? progPath, string? volsPath,
        string? equipagePath = null, AppSettings? settings = null)
    {
        _config = config;
        _output = output;
        _theme = theme;
        _settings = settings ?? new AppSettings();
        _paramPath = paramPath;
        _progPath = progPath;
        _volsPath = volsPath;
        _equipagePath = equipagePath;
    }

    public void ExecuteCommand(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var cmd = parts[0].ToLowerInvariant();
        try
        {
            switch (cmd)
            {
                case "calc":
                    HandleCalc();
                    break;
                case "show":
                    HandleShow(parts);
                    break;
                case "set":
                    HandleSet(parts);
                    break;
                case "add":
                    HandleAdd(parts);
                    break;
                case "del":
                    HandleDel(parts);
                    break;
                case "save":
                    HandleSave(parts);
                    break;
                case "load":
                    HandleLoad(parts);
                    break;
                case "new":
                    HandleNew();
                    break;
                case "help":
                    _output.WriteLine("Appuyez sur F1 pour l'aide detaillee.");
                    break;
                default:
                    _output.WriteLine($"Commande inconnue : {cmd}");
                    _output.WriteLine("Tapez 'help' ou F1 pour la liste des commandes.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"{_theme.AlertExceeded}Erreur: {ex.Message}{_theme.Reset}");
        }
    }

    // ── calc ──

    private void HandleCalc()
    {
        try
        {
            ConfigLoader.Valider(_config);
        }
        catch (ArgumentException ex)
        {
            _output.WriteLine($"{_theme.AlertExceeded}Configuration invalide: {ex.Message}{_theme.Reset}");
            return;
        }

        var resultat = CalculateurMarge.Calculer(_config);
        TuiRenderer.Afficher(resultat, _config, _output, _theme);
    }

    // ── show ──

    private void HandleShow(string[] parts)
    {
        var section = parts.Length > 1 ? parts[1].ToLowerInvariant() : "all";

        switch (section)
        {
            case "all":
                ShowEffectif();
                ShowFtl();
                ShowVols();
                ShowBlocs();
                ShowSemtypes();
                ShowCalendrier();
                ShowProgramme();
                ShowAbattements();
                ShowFonctionsSol();
                ShowCumul();
                ShowOff();
                break;
            case "effectif":
                ShowEffectif();
                break;
            case "ftl":
                ShowFtl();
                break;
            case "semtypes":
                ShowSemtypes();
                break;
            case "calendrier":
                ShowCalendrier();
                break;
            case "programme":
                ShowProgramme();
                break;
            case "abat":
                ShowAbattements();
                break;
            case "sol":
                ShowFonctionsSol();
                break;
            case "cumul":
                ShowCumul();
                break;
            case "vols":
                ShowVols();
                break;
            case "blocs":
                ShowBlocs();
                break;
            case "off":
                ShowOff();
                break;
            default:
                _output.WriteLine($"Section inconnue : {section}");
                _output.WriteLine("Sections : effectif, ftl, vols, blocs, semtypes, calendrier, programme, abat, sol, cumul, off");
                break;
        }
    }

    private void ShowEffectif()
    {
        _output.WriteLine("  EFFECTIF");
        _output.WriteLine($"    CDB ............. {_config.Effectif.Cdb}");
        _output.WriteLine($"    OPL ............. {_config.Effectif.Opl}");
        _output.WriteLine($"    CC  ............. {_config.Effectif.Cc}");
        _output.WriteLine($"    PNC ............. {_config.Effectif.Pnc}");
        _output.WriteLine($"    Periode ......... {_config.Periode.Mois} {_config.Periode.Annee} ({_config.Periode.NbJours} j)");
    }

    private void ShowFtl()
    {
        _output.WriteLine("  LIMITES FTL");
        _output.WriteLine($"    TSV max journalier .. {_config.LimitesFTL.TsvMaxJournalier:F1} h");
        _output.WriteLine($"    TSV moyen retenu .... {_config.LimitesFTL.TsvMoyenRetenu:F1} h");
        _output.WriteLine($"    Repos minimum ....... {_config.LimitesFTL.ReposMinimum:F1} h");
    }

    private void ShowProgramme()
    {
        var programme = CalendrierHelper.ResoudreProgramme(_config, out int nbSem, out _);
        _output.WriteLine($"  PROGRAMME RESOLU ({nbSem} semaines pour {_config.Periode.Mois} {_config.Periode.Annee})");
        _output.WriteLine($"  {"#",3} {"Seq",4} {"Jour",-10}{"Per.",-8}{"DP",14}{"FDP",14}{"Vols",5}{"HDV",7}");
        _output.WriteLine($"  {new string('-', 65)}");
        for (int i = 0; i < programme.Count; i++)
        {
            var b = programme[i];
            _output.WriteLine(
                $"  {i + 1,3} {b.Sequence,4} {b.Jour,-10}{b.Periode,-8}{b.DebutDP}-{b.FinDP,14}{b.DebutFDP}-{b.FinFDP,14}{b.NbEtapes,5}{b.HdvBloc,7:F2}");
        }

        if (programme.Count > 0)
        {
            _output.WriteLine("");
            _output.WriteLine("  Resume par jour :");
            var parJour = programme.GroupBy(b => b.JourIndex).OrderBy(g => g.Key);
            foreach (var g in parJour)
            {
                _output.WriteLine($"    {g.First().Jour,-10}: {g.Count()} bloc(s), {g.Sum(b => b.HdvBloc):F2}h HDV, {g.Sum(b => b.DureeTSHeures):F2}h DP");
            }
        }
    }

    private void ShowSemtypes()
    {
        _output.WriteLine("  SEMAINES TYPES");
        if (_config.SemainesTypes.Count == 0)
        {
            _output.WriteLine("    (aucune)");
            return;
        }

        var blocsDict = _config.CatalogueBlocs.ToDictionary(b => b.Id);
        foreach (var st in _config.SemainesTypes)
        {
            _output.WriteLine($"  {st.Reference} ({st.Saison}) - {st.Placements.Count} placement(s)");
            _output.WriteLine($"  {"#",3} {"Seq",4} {"Jour",-10}{"Code",-14}{"Per.",-8}{"DP",14}{"FDP",14}{"Vols",5}{"HDV",7}");
            _output.WriteLine($"  {new string('-', 79)}");
            var placements = st.Placements
                .OrderBy(p => HeureHelper.JourVersIndex(p.Jour))
                .ThenBy(p => p.Sequence)
                .ToList();
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (blocsDict.TryGetValue(p.BlocId, out var b))
                {
                    _output.WriteLine(
                        $"  {i + 1,3} {p.Sequence,4} {p.Jour,-10}{b.Code,-14}{b.Periode,-8}{b.DebutDP}-{b.FinDP,14}{b.DebutFDP}-{b.FinFDP,14}{b.NbEtapes,5}{b.HdvBloc,7:F2}");
                }
                else
                {
                    _output.WriteLine($"  {i + 1,3} {p.Sequence,4} {p.Jour,-10}(bloc inconnu: {p.BlocId})");
                }
            }
            _output.WriteLine("");
        }
    }

    private void ShowCalendrier()
    {
        _output.WriteLine($"  CALENDRIER {_config.Periode.Annee}");
        if (_config.Calendrier.Count == 0)
        {
            _output.WriteLine("    (vide)");
            return;
        }

        var stById = _config.SemainesTypes.ToDictionary(st => st.Id);
        var parAnnee = _config.Calendrier.OrderBy(a => a.Annee).ThenBy(a => a.Semaine);
        var sb = new StringBuilder();
        int col = 0;
        foreach (var a in parAnnee)
        {
            var refDisplay = stById.TryGetValue(a.SemaineTypeId, out var st)
                ? st.Reference
                : a.SemaineTypeRef;
            sb.Append($"  S{a.Semaine:D2} {refDisplay,-8}");
            col++;
            if (col >= 4)
            {
                _output.WriteLine(sb.ToString());
                sb.Clear();
                col = 0;
            }
        }
        if (sb.Length > 0)
            _output.WriteLine(sb.ToString());
    }

    private void ShowAbattements()
    {
        _output.WriteLine("  ABATTEMENTS PNT");
        for (int i = 0; i < _config.AbattementsPNT.Count; i++)
        {
            var a = _config.AbattementsPNT[i];
            _output.WriteLine($"    {i + 1}. {a.Libelle,-25} {a.JoursPersonnel} j");
        }

        if (_config.AbattementsPNT.Count == 0)
            _output.WriteLine("    (aucun)");

        _output.WriteLine("  ABATTEMENTS PNC");
        for (int i = 0; i < _config.AbattementsPNC.Count; i++)
        {
            var a = _config.AbattementsPNC[i];
            _output.WriteLine($"    {i + 1}. {a.Libelle,-25} {a.JoursPersonnel} j");
        }

        if (_config.AbattementsPNC.Count == 0)
            _output.WriteLine("    (aucun)");
    }

    private void ShowFonctionsSol()
    {
        _output.WriteLine("  FONCTIONS SOL PNT");
        for (int i = 0; i < _config.FonctionsSolPNT.Count; i++)
        {
            var f = _config.FonctionsSolPNT[i];
            _output.WriteLine($"    {i + 1}. {f.Nom,-20} {f.NbPersonnes} pers x {f.JoursSolMois} j = {f.JoursPersonnelSol} j");
        }

        if (_config.FonctionsSolPNT.Count == 0)
            _output.WriteLine("    (aucun)");

        _output.WriteLine("  FONCTIONS SOL PNC");
        for (int i = 0; i < _config.FonctionsSolPNC.Count; i++)
        {
            var f = _config.FonctionsSolPNC[i];
            _output.WriteLine($"    {i + 1}. {f.Nom,-20} {f.NbPersonnes} pers x {f.JoursSolMois} j = {f.JoursPersonnelSol} j");
        }

        if (_config.FonctionsSolPNC.Count == 0)
            _output.WriteLine("    (aucun)");
    }

    private void ShowCumul()
    {
        _output.WriteLine("  LIMITES CUMULATIVES");
        _output.WriteLine($"    H28 max ......... {_config.LimitesCumulatives.H28Max:F0} h");
        _output.WriteLine($"    H90 max ......... {_config.LimitesCumulatives.H90Max:F0} h");
        _output.WriteLine($"    H12 max ......... {_config.LimitesCumulatives.H12Max:F0} h");
        _output.WriteLine("  COMPTEURS ENTRANTS PNT");
        _output.WriteLine($"    Cumul 28j ....... {_config.LimitesCumulatives.CumulPNT.Cumul28Entrant:F1} h");
        _output.WriteLine($"    Cumul 90j ....... {_config.LimitesCumulatives.CumulPNT.Cumul90Entrant:F1} h");
        _output.WriteLine($"    Cumul 12m ....... {_config.LimitesCumulatives.CumulPNT.Cumul12Entrant:F1} h");
        _output.WriteLine("  COMPTEURS ENTRANTS PNC");
        _output.WriteLine($"    Cumul 28j ....... {_config.LimitesCumulatives.CumulPNC.Cumul28Entrant:F1} h");
        _output.WriteLine($"    Cumul 90j ....... {_config.LimitesCumulatives.CumulPNC.Cumul90Entrant:F1} h");
        _output.WriteLine($"    Cumul 12m ....... {_config.LimitesCumulatives.CumulPNC.Cumul12Entrant:F1} h");
    }

    private void ShowVols()
    {
        _output.WriteLine("  CATALOGUE VOLS");
        if (_config.CatalogueVols.Count == 0)
        {
            _output.WriteLine("    (vide)");
            return;
        }

        _output.WriteLine($"  {"#",3} {"Numero",-8}{"Dep",-5}{"Arr",-5}{"HDep",-7}{"HArr",-7}{"HDV",6}");
        _output.WriteLine($"  {new string('-', 41)}");
        for (int i = 0; i < _config.CatalogueVols.Count; i++)
        {
            var v = _config.CatalogueVols[i];
            _output.WriteLine($"  {i + 1,3} {v.Numero,-8}{v.Depart,-5}{v.Arrivee,-5}{v.HeureDepart,-7}{v.HeureArrivee,-7}{v.HdvVol,6:F2}");
        }
    }

    private void ShowBlocs()
    {
        _output.WriteLine("  CATALOGUE BLOCS");
        if (_config.CatalogueBlocs.Count == 0)
        {
            _output.WriteLine("    (vide)");
            return;
        }

        // Compter les placements par bloc
        var nbPlacements = new Dictionary<Guid, int>();
        foreach (var st in _config.SemainesTypes)
            foreach (var p in st.Placements)
                nbPlacements[p.BlocId] = nbPlacements.GetValueOrDefault(p.BlocId) + 1;

        _output.WriteLine($"  {"#",3} {"Code",-16}{"Per.",-8}{"DP",14}{"FDP",14}{"Vols",5}{"HDV",7}{"Plac.",6}");
        _output.WriteLine($"  {new string('-', 73)}");
        for (int i = 0; i < _config.CatalogueBlocs.Count; i++)
        {
            var b = _config.CatalogueBlocs[i];
            var plac = nbPlacements.GetValueOrDefault(b.Id);
            _output.WriteLine(
                $"  {i + 1,3} {b.Code,-16}{b.Periode,-8}{b.DebutDP}-{b.FinDP,14}{b.DebutFDP}-{b.FinFDP,14}{b.NbEtapes,5}{b.HdvBloc,7:F2}{plac,6}");
        }
    }

    private void ShowOff()
    {
        _output.WriteLine("  JOURS OFF");
        _output.WriteLine($"    Reglementaire ....... {_config.JoursOff.Reglementaire} j");
        _output.WriteLine($"    Accord entreprise ... {_config.JoursOff.AccordEntreprise} j");
        _output.WriteLine($"    Total ............... {_config.JoursOff.Reglementaire + _config.JoursOff.AccordEntreprise} j");
    }

    // ── set ──

    private void HandleSet(string[] parts)
    {
        if (parts.Length < 3)
        {
            _output.WriteLine("Usage: set <parametre> <valeur>");
            _output.WriteLine("Tapez 'help' pour la liste des parametres.");
            return;
        }

        var param = parts[1].ToLowerInvariant();
        switch (param)
        {
            case "cdb":
                _config.Effectif.Cdb = ParseInt(parts[2]);
                Ok($"CDB = {_config.Effectif.Cdb}");
                break;
            case "opl":
                _config.Effectif.Opl = ParseInt(parts[2]);
                Ok($"OPL = {_config.Effectif.Opl}");
                break;
            case "cc":
                _config.Effectif.Cc = ParseInt(parts[2]);
                Ok($"CC = {_config.Effectif.Cc}");
                break;
            case "pnc":
                _config.Effectif.Pnc = ParseInt(parts[2]);
                Ok($"PNC = {_config.Effectif.Pnc}");
                break;
            case "cal":
                HandleSetCal(parts);
                break;
            case "tsv":
                _config.LimitesFTL.TsvMoyenRetenu = ParseDbl(parts[2]);
                Ok($"TSV moyen = {_config.LimitesFTL.TsvMoyenRetenu:F1}");
                break;
            case "tsvmax":
                _config.LimitesFTL.TsvMaxJournalier = ParseDbl(parts[2]);
                Ok($"TSV max = {_config.LimitesFTL.TsvMaxJournalier:F1}");
                break;
            case "repos":
                _config.LimitesFTL.ReposMinimum = ParseDbl(parts[2]);
                Ok($"Repos min = {_config.LimitesFTL.ReposMinimum:F1}");
                break;
            case "mois":
                if (parts.Length < 5)
                {
                    _output.WriteLine("Usage: set mois <nom> <annee> <nbJours>");
                    return;
                }
                _config.Periode.Mois = parts[2];
                _config.Periode.Annee = ParseInt(parts[3]);
                _config.Periode.NbJours = ParseInt(parts[4]);
                Ok($"Periode = {_config.Periode.Mois} {_config.Periode.Annee} ({_config.Periode.NbJours} j)");
                break;
            case "off":
                if (parts.Length < 4)
                {
                    _output.WriteLine("Usage: set off <reglementaire> <accord>");
                    return;
                }
                _config.JoursOff.Reglementaire = ParseInt(parts[2]);
                _config.JoursOff.AccordEntreprise = ParseInt(parts[3]);
                Ok($"OFF = {_config.JoursOff.Reglementaire} + {_config.JoursOff.AccordEntreprise}");
                break;
            case "h28max":
                _config.LimitesCumulatives.H28Max = ParseDbl(parts[2]);
                Ok($"H28 max = {_config.LimitesCumulatives.H28Max:F0}");
                break;
            case "h90max":
                _config.LimitesCumulatives.H90Max = ParseDbl(parts[2]);
                Ok($"H90 max = {_config.LimitesCumulatives.H90Max:F0}");
                break;
            case "h12max":
                _config.LimitesCumulatives.H12Max = ParseDbl(parts[2]);
                Ok($"H12 max = {_config.LimitesCumulatives.H12Max:F0}");
                break;
            case "cumul":
                HandleSetCumul(parts);
                break;
            case "srcdir":
                var dirPath = string.Join(" ", parts.Skip(2));
                if (string.IsNullOrWhiteSpace(dirPath))
                {
                    _output.WriteLine($"Repertoire de sources : {_settings.RepertoireEffectif}");
                }
                else if (Directory.Exists(dirPath))
                {
                    _settings.RepertoireSources = Path.GetFullPath(dirPath);
                    _settings.Sauvegarder();
                    _output.WriteLine($"{_theme.AlertOk}Repertoire de sources = {_settings.RepertoireSources}{_theme.Reset}");
                }
                else
                {
                    _output.WriteLine($"{_theme.AlertExceeded}Repertoire introuvable : {dirPath}{_theme.Reset}");
                }
                return; // pas de MarkDirty
            default:
                _output.WriteLine($"Parametre inconnu : {param}");
                break;
        }
    }

    private void HandleSetCumul(string[] parts)
    {
        // set cumul pnt|pnc 28|90|12 <valeur>
        if (parts.Length < 5)
        {
            _output.WriteLine("Usage: set cumul pnt|pnc 28|90|12 <valeur>");
            return;
        }

        var group = parts[2].ToLowerInvariant();
        var period = parts[3];
        var val = ParseDbl(parts[4]);

        CumulEntrant cumul = group switch
        {
            "pnt" => _config.LimitesCumulatives.CumulPNT,
            "pnc" => _config.LimitesCumulatives.CumulPNC,
            _ => throw new ArgumentException($"Groupe inconnu : {group} (pnt ou pnc)")
        };

        switch (period)
        {
            case "28":
                cumul.Cumul28Entrant = val;
                break;
            case "90":
                cumul.Cumul90Entrant = val;
                break;
            case "12":
                cumul.Cumul12Entrant = val;
                break;
            default:
                _output.WriteLine($"Periode inconnue : {period} (28, 90 ou 12)");
                return;
        }

        Ok($"Cumul {group.ToUpper()} {period} = {val:F1}");
    }

    private void HandleSetCal(string[] parts)
    {
        // set cal <semaine|sem1-sem2> <annee> <ref>
        if (parts.Length < 5)
        {
            _output.WriteLine("Usage: set cal <semaine|sem1-sem2> <annee> <ref>");
            _output.WriteLine("  set cal 10 2026 BS_01");
            _output.WriteLine("  set cal 10-14 2026 BS_01");
            return;
        }

        var semaineArg = parts[2];
        var annee = ParseInt(parts[3]);
        var refSt = parts[4];

        // Vérifier que la référence existe et récupérer l'Id
        var st = _config.SemainesTypes.FirstOrDefault(s => s.Reference == refSt);
        if (st == null)
        {
            _output.WriteLine($"Semaine type inconnue : '{refSt}'");
            return;
        }

        int debut, fin;
        if (semaineArg.Contains('-'))
        {
            var rangeParts = semaineArg.Split('-');
            debut = ParseInt(rangeParts[0]);
            fin = ParseInt(rangeParts[1]);
        }
        else
        {
            debut = ParseInt(semaineArg);
            fin = debut;
        }

        int count = 0;
        for (int s = debut; s <= fin; s++)
        {
            var existing = _config.Calendrier.FirstOrDefault(a => a.Semaine == s && a.Annee == annee);
            if (existing != null)
            {
                existing.SemaineTypeId = st.Id;
                existing.SemaineTypeRef = refSt;
            }
            else
            {
                _config.Calendrier.Add(new AffectationSemaine
                {
                    Semaine = s, Annee = annee,
                    SemaineTypeId = st.Id, SemaineTypeRef = refSt
                });
            }
            count++;
        }

        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Calendrier : S{debut:D2}-S{fin:D2} {annee} → {refSt} ({count} semaine(s)){_theme.Reset}");
    }

    // ── add ──

    private void HandleAdd(string[] parts)
    {
        if (parts.Length < 2)
        {
            _output.WriteLine("Usage: add vol|bloc|placement|semtype|abat|sol ...");
            return;
        }

        var type = parts[1].ToLowerInvariant();
        switch (type)
        {
            case "vol":
                HandleAddVol(parts);
                break;
            case "semtype":
                HandleAddSemtype(parts);
                break;
            case "bloc":
                HandleAddBloc(parts);
                break;
            case "placement":
                HandleAddPlacement(parts);
                break;
            case "abat":
                HandleAddAbattement(parts);
                break;
            case "sol":
                HandleAddSol(parts);
                break;
            default:
                _output.WriteLine($"Type inconnu : {type} (vol, semtype, bloc, placement, abat, sol)");
                break;
        }
    }

    private void HandleAddVol(string[] parts)
    {
        // add vol <numero> <depart> <arrivee> <heureDepart> <heureArrivee>
        if (parts.Length < 7)
        {
            _output.WriteLine("Usage: add vol <numero> <depart> <arrivee> <heureDepart> <heureArrivee>");
            _output.WriteLine("  Exemple: add vol 201 NOU LIF 07:00 07:40");
            return;
        }

        var vol = new Vol
        {
            Numero = parts[2],
            Depart = parts[3],
            Arrivee = parts[4],
            HeureDepart = parts[5],
            HeureArrivee = parts[6]
        };

        _config.CatalogueVols.Add(vol);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Vol ajoute : {vol.Numero} {vol.Depart}-{vol.Arrivee} {vol.HeureDepart}-{vol.HeureArrivee} ({vol.HdvVol:F2}h){_theme.Reset}");
    }

    private void HandleAddSemtype(string[] parts)
    {
        // add semtype <ref> <saison>
        if (parts.Length < 4)
        {
            _output.WriteLine("Usage: add semtype <reference> <saison>");
            _output.WriteLine("  add semtype HS_01 Haute");
            return;
        }

        var reference = parts[2];
        var saison = parts[3];

        if (_config.SemainesTypes.Any(st => st.Reference == reference))
        {
            _output.WriteLine($"Semaine type '{reference}' existe deja.");
            return;
        }

        _config.SemainesTypes.Add(new SemaineType { Reference = reference, Saison = saison });
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Semaine type ajoutee : {reference} ({saison}){_theme.Reset}");
    }

    private void HandleAddBloc(string[] parts)
    {
        // add bloc <ref> <seq> <jour> <periode> <debutDP> <finDP> <debutFDP> <finFDP> <num-dep-arr-hdep-harr> [...]
        if (parts.Length < 11)
        {
            _output.WriteLine("Usage: add bloc <ref> <seq> <jour> <periode> <debutDP> <finDP> <debutFDP> <finFDP> <vol> [...]");
            _output.WriteLine("  Vol: num-dep-arr-hdep-harr  (ex: 201-NOU-LIF-07:00-07:40)");
            _output.WriteLine("  Exemple: add bloc BS_01 1 Lundi matin 06:10 11:50 06:10 11:30 201-NOU-LIF-07:00-07:40");
            return;
        }

        var refSt = parts[2];
        var st = _config.SemainesTypes.FirstOrDefault(s => s.Reference == refSt);
        if (st == null)
        {
            _output.WriteLine($"Semaine type inconnue : '{refSt}'");
            return;
        }

        var sequence = ParseInt(parts[3]);
        var jour = parts[4];

        // 1. Résoudre ou créer les vols dans le catalogue
        var etapes = new List<EtapeVol>();
        int position = 1;
        for (int i = 10; i < parts.Length; i++)
        {
            var volParts = parts[i].Split('-');
            if (volParts.Length < 5)
            {
                _output.WriteLine($"Format vol invalide : {parts[i]} (attendu num-dep-arr-hdep-harr)");
                return;
            }

            var numero = volParts[0];
            var depart = volParts[1];
            var arrivee = volParts[2];
            var hDep = volParts[3];
            var hArr = volParts[4];

            // Chercher un vol existant correspondant dans le catalogue
            var volExistant = _config.CatalogueVols.FirstOrDefault(v =>
                v.Numero == numero && v.Depart == depart && v.Arrivee == arrivee
                && v.HeureDepart == hDep && v.HeureArrivee == hArr);

            if (volExistant == null)
            {
                volExistant = new Vol
                {
                    Numero = numero, Depart = depart, Arrivee = arrivee,
                    HeureDepart = hDep, HeureArrivee = hArr
                };
                _config.CatalogueVols.Add(volExistant);
            }

            etapes.Add(new EtapeVol { Position = position++, VolId = volExistant.Id });
        }

        // 2. Créer le bloc dans le catalogue
        var bloc = new BlocVol
        {
            Periode = parts[5],
            DebutDP = parts[6],
            FinDP = parts[7],
            DebutFDP = parts[8],
            FinFDP = parts[9],
            Etapes = etapes
        };
        // Générer un code unique
        var firstVol = etapes.Count > 0
            ? _config.CatalogueVols.FirstOrDefault(v => v.Id == etapes[0].VolId)
            : null;
        var dest = firstVol?.Arrivee ?? "???";
        var codeBase = $"ROT-{dest}-{bloc.Periode}";
        var code = codeBase;
        int suffix = 2;
        while (_config.CatalogueBlocs.Any(b => b.Code == code))
            code = $"{codeBase}-{suffix++}";
        bloc.Code = code;

        _config.CatalogueBlocs.Add(bloc);

        // 3. Créer le placement dans la semaine type
        st.Placements.Add(new BlocPlacement
        {
            BlocId = bloc.Id,
            Jour = jour,
            Sequence = sequence
        });

        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Bloc '{bloc.Code}' cree dans le catalogue et place dans {refSt} : seq {sequence} {jour} ({etapes.Count} vols){_theme.Reset}");
    }

    private void HandleAddPlacement(string[] parts)
    {
        // add placement <stRef> <seq> <jour> <blocCode>
        if (parts.Length < 6)
        {
            _output.WriteLine("Usage: add placement <stRef> <sequence> <jour> <blocCode>");
            _output.WriteLine("  Exemple: add placement BS_01 1 Lundi ROT-LIF-AM");
            return;
        }

        var refSt = parts[2];
        var st = _config.SemainesTypes.FirstOrDefault(s => s.Reference == refSt);
        if (st == null)
        {
            _output.WriteLine($"Semaine type inconnue : '{refSt}'");
            return;
        }

        var sequence = ParseInt(parts[3]);
        var jour = parts[4];
        var blocCode = parts[5];

        if (HeureHelper.JourVersIndex(jour) == 0)
        {
            _output.WriteLine($"Jour invalide : '{jour}' (Lundi..Dimanche)");
            return;
        }

        var bloc = _config.CatalogueBlocs.FirstOrDefault(b => b.Code == blocCode);
        if (bloc == null)
        {
            _output.WriteLine($"Bloc inconnu : '{blocCode}'");
            _output.WriteLine("Blocs disponibles : " +
                string.Join(", ", _config.CatalogueBlocs.Select(b => b.Code)));
            return;
        }

        st.Placements.Add(new BlocPlacement
        {
            BlocId = bloc.Id,
            Jour = jour,
            Sequence = sequence
        });

        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Placement ajoute dans {refSt} : seq {sequence} {jour} → {blocCode}{_theme.Reset}");
    }

    private void HandleAddAbattement(string[] parts)
    {
        // add abat pnt|pnc <libelle> <jours>
        if (parts.Length < 5)
        {
            _output.WriteLine("Usage: add abat pnt|pnc <libelle> <jours>");
            return;
        }

        var group = parts[2].ToLowerInvariant();
        var libelle = parts[3].Replace('_', ' ');
        var jours = ParseInt(parts[4]);

        var abat = new Abattement { Libelle = libelle, JoursPersonnel = jours };

        switch (group)
        {
            case "pnt":
                _config.AbattementsPNT.Add(abat);
                break;
            case "pnc":
                _config.AbattementsPNC.Add(abat);
                break;
            default:
                _output.WriteLine($"Groupe inconnu : {group} (pnt ou pnc)");
                return;
        }

        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Abattement {group.ToUpper()} ajoute : {libelle} ({jours} j){_theme.Reset}");
    }

    private void HandleAddSol(string[] parts)
    {
        // add sol pnt|pnc <nom> <nbPersonnes> <joursMois>
        if (parts.Length < 6)
        {
            _output.WriteLine("Usage: add sol pnt|pnc <nom> <nbPersonnes> <joursMois>");
            return;
        }

        var group = parts[2].ToLowerInvariant();
        var nom = parts[3].Replace('_', ' ');
        var nbPers = ParseInt(parts[4]);
        var joursMois = ParseInt(parts[5]);

        var fonc = new FonctionSol { Nom = nom, NbPersonnes = nbPers, JoursSolMois = joursMois };

        switch (group)
        {
            case "pnt":
                _config.FonctionsSolPNT.Add(fonc);
                break;
            case "pnc":
                _config.FonctionsSolPNC.Add(fonc);
                break;
            default:
                _output.WriteLine($"Groupe inconnu : {group} (pnt ou pnc)");
                return;
        }

        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Fonction sol {group.ToUpper()} ajoutee : {nom} ({nbPers} x {joursMois} j){_theme.Reset}");
    }

    // ── del ──

    private void HandleDel(string[] parts)
    {
        if (parts.Length < 3)
        {
            _output.WriteLine("Usage: del vol|bloc|placement|semtype|abat|sol ...");
            return;
        }

        var type = parts[1].ToLowerInvariant();
        switch (type)
        {
            case "vol":
                HandleDelVol(parts);
                break;
            case "semtype":
                HandleDelSemtype(parts);
                break;
            case "bloc":
                HandleDelBloc(parts);
                break;
            case "placement":
                HandleDelPlacement(parts);
                break;
            case "abat":
                HandleDelAbat(parts);
                break;
            case "sol":
                HandleDelSol(parts);
                break;
            default:
                _output.WriteLine($"Type inconnu : {type} (vol, semtype, bloc, placement, abat, sol)");
                break;
        }
    }

    private void HandleDelVol(string[] parts)
    {
        // del vol <index>
        if (parts.Length < 3)
        {
            _output.WriteLine("Usage: del vol <index>");
            _output.WriteLine("  Utilisez 'show vols' pour voir les index.");
            return;
        }

        int idx = ParseInt(parts[2]) - 1;
        if (idx < 0 || idx >= _config.CatalogueVols.Count)
        {
            _output.WriteLine($"Index invalide : {idx + 1} (1..{_config.CatalogueVols.Count})");
            return;
        }

        var vol = _config.CatalogueVols[idx];

        // Vérifier qu'aucun bloc ne référence ce vol
        var blocsRefs = _config.CatalogueBlocs
            .Where(b => b.Etapes.Any(e => e.VolId == vol.Id))
            .Select(b => b.Code)
            .ToList();
        if (blocsRefs.Count > 0)
        {
            _output.WriteLine($"Impossible : vol reference par les blocs : {string.Join(", ", blocsRefs)}");
            return;
        }

        _config.CatalogueVols.RemoveAt(idx);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Vol #{idx + 1} supprime : {vol.Numero} {vol.Depart}-{vol.Arrivee}{_theme.Reset}");
    }

    private void HandleDelPlacement(string[] parts)
    {
        // del placement <stRef> <index>
        if (parts.Length < 4)
        {
            _output.WriteLine("Usage: del placement <stRef> <index>");
            _output.WriteLine("  Utilisez 'show semtypes' pour voir les index.");
            return;
        }

        var refSt = parts[2];
        var st = _config.SemainesTypes.FirstOrDefault(s => s.Reference == refSt);
        if (st == null)
        {
            _output.WriteLine($"Semaine type inconnue : '{refSt}'");
            return;
        }

        int idx = ParseInt(parts[3]) - 1;
        var placements = st.Placements
            .OrderBy(p => HeureHelper.JourVersIndex(p.Jour))
            .ThenBy(p => p.Sequence)
            .ToList();

        if (idx < 0 || idx >= placements.Count)
        {
            _output.WriteLine($"Index invalide : {idx + 1} (1..{placements.Count})");
            return;
        }

        st.Placements.Remove(placements[idx]);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Placement #{idx + 1} supprime de {refSt}.{_theme.Reset}");
    }

    private void HandleDelSemtype(string[] parts)
    {
        // del semtype <ref>
        if (parts.Length < 3)
        {
            _output.WriteLine("Usage: del semtype <reference>");
            return;
        }

        var refSt = parts[2];
        var st = _config.SemainesTypes.FirstOrDefault(s => s.Reference == refSt);
        if (st == null)
        {
            _output.WriteLine($"Semaine type inconnue : '{refSt}'");
            return;
        }

        _config.SemainesTypes.Remove(st);
        // Retirer les affectations calendrier qui référencent cette semaine type (par Id)
        _config.Calendrier.RemoveAll(a => a.SemaineTypeId == st.Id);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Semaine type '{refSt}' supprimee (+ affectations calendrier){_theme.Reset}");
    }

    private void HandleDelBloc(string[] parts)
    {
        // del bloc <code>
        if (parts.Length < 3)
        {
            _output.WriteLine("Usage: del bloc <code>");
            _output.WriteLine("  Supprime le bloc du catalogue et tous ses placements.");
            return;
        }

        var code = parts[2];
        var bloc = _config.CatalogueBlocs.FirstOrDefault(b => b.Code == code);
        if (bloc == null)
        {
            _output.WriteLine($"Bloc inconnu : '{code}'");
            return;
        }

        // Retirer tous les placements qui référencent ce bloc
        int nbPlacements = 0;
        foreach (var st in _config.SemainesTypes)
        {
            nbPlacements += st.Placements.RemoveAll(p => p.BlocId == bloc.Id);
        }

        _config.CatalogueBlocs.Remove(bloc);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}Bloc '{code}' supprime du catalogue ({nbPlacements} placement(s) retires).{_theme.Reset}");
    }

    private void HandleDelAbat(string[] parts)
    {
        // del abat pnt|pnc <index>
        if (parts.Length < 4)
        {
            _output.WriteLine("Usage: del abat pnt|pnc <index>");
            return;
        }

        var group = parts[2].ToLowerInvariant();
        switch (group)
        {
            case "pnt":
                DelFromList(parts, _config.AbattementsPNT, "Abattement PNT", 3);
                break;
            case "pnc":
                DelFromList(parts, _config.AbattementsPNC, "Abattement PNC", 3);
                break;
            default:
                _output.WriteLine($"Groupe inconnu : {group} (pnt ou pnc)");
                break;
        }
    }

    private void HandleDelSol(string[] parts)
    {
        // del sol pnt|pnc <index>
        if (parts.Length < 4)
        {
            _output.WriteLine("Usage: del sol pnt|pnc <index>");
            return;
        }

        var group = parts[2].ToLowerInvariant();
        switch (group)
        {
            case "pnt":
                DelFromList(parts, _config.FonctionsSolPNT, "Fonction sol PNT", 3);
                break;
            case "pnc":
                DelFromList(parts, _config.FonctionsSolPNC, "Fonction sol PNC", 3);
                break;
            default:
                _output.WriteLine($"Groupe inconnu : {group} (pnt ou pnc)");
                break;
        }
    }

    private void DelFromList<T>(string[] parts, List<T> list, string label, int indexArg)
    {
        if (parts.Length <= indexArg)
        {
            _output.WriteLine($"Usage: del ... <index> (1-based)");
            return;
        }

        int idx = ParseInt(parts[indexArg]) - 1;
        if (idx < 0 || idx >= list.Count)
        {
            _output.WriteLine($"Index invalide : {idx + 1} (1..{list.Count})");
            return;
        }

        list.RemoveAt(idx);
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}{label} #{idx + 1} supprime.{_theme.Reset}");
    }

    // ── save ──

    private void HandleSave(string[] parts)
    {
        if (parts.Length > 1)
        {
            var baseName = Path.GetFileNameWithoutExtension(parts[1]);
            var dir = _settings.RepertoireEffectif;
            _paramPath = Path.Combine(dir, $"{baseName}.params.xml");
            _progPath = Path.Combine(dir, $"{baseName}.prog.xml");
            _volsPath = Path.Combine(dir, $"{baseName}.vols.xml");
            if (_config.Equipage != null)
                _equipagePath = Path.Combine(dir, $"{baseName}.equip.xml");
        }
        else if (_paramPath == null)
        {
            _output.WriteLine("Usage: save <nom_base>");
            return;
        }

        AutoSave();
        var msg = $"{Path.GetFileName(_paramPath!)} / {Path.GetFileName(_progPath!)} / {Path.GetFileName(_volsPath!)}";
        if (_equipagePath != null)
            msg += $" / {Path.GetFileName(_equipagePath)}";
        _output.WriteLine($"{_theme.AlertOk}Sauvegarde : {msg}{_theme.Reset}");
    }

    // ── load ──

    private void HandleLoad(string[] parts)
    {
        if (parts.Length < 2)
        {
            _output.WriteLine("Usage: load <chemin.xml>");
            return;
        }

        var path = parts[1];
        var type = XmlConfigLoader.DetecterType(path);

        switch (type)
        {
            case ConfigFileType.Legacy:
                _config = XmlConfigLoader.Charger(path);
                _paramPath = path;
                _progPath = null;
                _volsPath = null;
                _dirty = false;
                _output.WriteLine($"{_theme.AlertOk}Configuration legacy chargee : {path}{_theme.Reset}");
                break;
            case ConfigFileType.Parametres:
                XmlConfigLoader.MergerParametres(_config, XmlConfigLoader.ChargerParametres(path));
                _paramPath = path;
                _dirty = false;
                _output.WriteLine($"{_theme.AlertOk}Parametres charges : {path}{_theme.Reset}");
                break;
            case ConfigFileType.Programme:
                XmlConfigLoader.MergerProgramme(_config, XmlConfigLoader.ChargerProgramme(path));
                _progPath = path;
                _dirty = false;
                _output.WriteLine($"{_theme.AlertOk}Programme charge : {path}{_theme.Reset}");
                break;
            case ConfigFileType.CatalogueVols:
                XmlConfigLoader.MergerCatalogueVols(_config, XmlConfigLoader.ChargerCatalogueVols(path));
                _volsPath = path;
                _dirty = false;
                _output.WriteLine($"{_theme.AlertOk}Catalogue vols charge : {path}{_theme.Reset}");
                break;
            case ConfigFileType.Equipage:
                XmlConfigLoader.MergerEquipage(_config, XmlConfigLoader.ChargerEquipage(path));
                _equipagePath = path;
                _dirty = false;
                _output.WriteLine($"{_theme.AlertOk}Equipage charge : {path}{_theme.Reset}");
                break;
            default:
                _output.WriteLine($"{_theme.AlertExceeded}Fichier XML non reconnu : {path}{_theme.Reset}");
                break;
        }
    }

    // ── new ──

    private void HandleNew()
    {
        _config = new Configuration();
        _paramPath = null;
        _progPath = null;
        _volsPath = null;
        _equipagePath = null;
        _dirty = false;
        _output.WriteLine($"{_theme.AlertOk}Configuration reinitialisee.{_theme.Reset}");
    }

    // ── Public file operations (for FileMenuOverlay) ──

    public void LoadConfig(string path, ConfigFileType type)
    {
        switch (type)
        {
            case ConfigFileType.Legacy:
                _config = XmlConfigLoader.Charger(path);
                _paramPath = path;
                _progPath = null;
                _volsPath = null;
                break;
            case ConfigFileType.Parametres:
                XmlConfigLoader.MergerParametres(_config, XmlConfigLoader.ChargerParametres(path));
                _paramPath = path;
                break;
            case ConfigFileType.Programme:
                XmlConfigLoader.MergerProgramme(_config, XmlConfigLoader.ChargerProgramme(path));
                _progPath = path;
                break;
            case ConfigFileType.CatalogueVols:
                XmlConfigLoader.MergerCatalogueVols(_config, XmlConfigLoader.ChargerCatalogueVols(path));
                _volsPath = path;
                break;
            case ConfigFileType.Equipage:
                XmlConfigLoader.MergerEquipage(_config, XmlConfigLoader.ChargerEquipage(path));
                _equipagePath = path;
                break;
        }
        _dirty = false;
    }

    public void SaveConfigAs(string path, ConfigFileType type)
    {
        if (!Path.HasExtension(path) || Path.GetExtension(path).ToLowerInvariant() != ".xml")
            path = Path.ChangeExtension(path, ".xml");

        switch (type)
        {
            case ConfigFileType.Parametres:
                XmlConfigLoader.SauvegarderParametres(path, _config);
                _paramPath = path;
                break;
            case ConfigFileType.Programme:
                XmlConfigLoader.SauvegarderProgramme(path, _config);
                _progPath = path;
                break;
            case ConfigFileType.CatalogueVols:
                XmlConfigLoader.SauvegarderCatalogueVols(path, _config);
                _volsPath = path;
                break;
            case ConfigFileType.Equipage:
                if (_config.Equipage != null)
                {
                    XmlConfigLoader.SauvegarderEquipage(path, _config.Equipage);
                    _equipagePath = path;
                }
                break;
        }
        _dirty = false;
    }

    public void NewConfig()
    {
        _config = new Configuration();
        _paramPath = null;
        _progPath = null;
        _volsPath = null;
        _equipagePath = null;
        _dirty = false;
    }

    // ── Helpers ──

    private void Ok(string msg)
    {
        MarkDirty();
        _output.WriteLine($"{_theme.AlertOk}{msg}{_theme.Reset}");
    }

    private static int ParseInt(string s)
    {
        if (int.TryParse(s, NumberStyles.Integer, Inv, out var v))
            return v;
        throw new ArgumentException($"Valeur entiere invalide : {s}");
    }

    private static double ParseDbl(string s)
    {
        if (double.TryParse(s, NumberStyles.Float, Inv, out var v))
            return v;
        throw new ArgumentException($"Valeur numerique invalide : {s}");
    }
}
