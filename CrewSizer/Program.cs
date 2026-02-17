using CrewSizer.IO;
using CrewSizer.Models;
using CrewSizer.Services;
using CrewSizer.Tui;

// ── Parsing arguments ──
string? configPath = null;
string? paramPath = null;
string? progPath = null;
string? volsPath = null;
string? equipPath = null;
bool quiet = false;
bool batch = false;
string style = "borland";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-c" or "--config" when i + 1 < args.Length:
            configPath = args[++i];
            batch = true;
            break;
        case "--param" when i + 1 < args.Length:
            paramPath = args[++i];
            break;
        case "--prog" when i + 1 < args.Length:
            progPath = args[++i];
            break;
        case "--vols" when i + 1 < args.Length:
            volsPath = args[++i];
            break;
        case "--equip" when i + 1 < args.Length:
            equipPath = args[++i];
            break;
        case "-q" or "--quiet":
            quiet = true;
            batch = true;
            break;
        case "--batch":
            batch = true;
            break;
        case "--tui":
            batch = false;
            break;
        case "--style" when i + 1 < args.Length:
            style = args[++i].ToLowerInvariant();
            break;
        case "--srcdir" when i + 1 < args.Length:
            var srcDirArg = Path.GetFullPath(args[++i]);
            if (Directory.Exists(srcDirArg))
            {
                var s = AppSettings.Charger();
                s.RepertoireSources = srcDirArg;
                s.Sauvegarder();
            }
            break;
        case "--help":
            AfficherAide();
            return 0;
        default:
            // Positional arg: auto-detect file type
            if (!args[i].StartsWith('-') && File.Exists(args[i]))
            {
                var type = XmlConfigLoader.DetecterType(args[i]);
                switch (type)
                {
                    case ConfigFileType.Parametres:
                        paramPath = args[i];
                        break;
                    case ConfigFileType.Programme:
                        progPath = args[i];
                        break;
                    case ConfigFileType.CatalogueVols:
                        volsPath = args[i];
                        break;
                    case ConfigFileType.Equipage:
                        equipPath = args[i];
                        break;
                    case ConfigFileType.Legacy:
                        configPath = args[i];
                        break;
                    default:
                        configPath = args[i];
                        break;
                }
            }
            break;
    }
}

// ── Mode batch ──
if (batch)
{
    return RunBatch(configPath ?? Path.Combine(AppContext.BaseDirectory, "Config", "Parametres.xml"), quiet);
}

// ── Mode TUI ──
try
{
    var settings = AppSettings.Charger();

    // Auto-chargement depuis le répertoire de sources (si aucun arg CLI)
    if (paramPath == null && progPath == null && volsPath == null
        && equipPath == null && configPath == null)
    {
        if (!string.IsNullOrWhiteSpace(settings.RepertoireSources)
            && Directory.Exists(settings.RepertoireSources))
        {
            foreach (var f in Directory.GetFiles(settings.RepertoireSources, "*.xml"))
            {
                var type = XmlConfigLoader.DetecterType(f);
                switch (type)
                {
                    case ConfigFileType.Parametres when paramPath == null:
                        paramPath = f; break;
                    case ConfigFileType.Programme when progPath == null:
                        progPath = f; break;
                    case ConfigFileType.CatalogueVols when volsPath == null:
                        volsPath = f; break;
                    case ConfigFileType.Equipage when equipPath == null:
                        equipPath = f; break;
                }
            }
        }

        // Fallback : derniers fichiers connus
        if (paramPath == null && settings.DernierParametres != null && File.Exists(settings.DernierParametres))
            paramPath = settings.DernierParametres;
        if (progPath == null && settings.DernierProgramme != null && File.Exists(settings.DernierProgramme))
            progPath = settings.DernierProgramme;
        if (volsPath == null && settings.DernierCatalogueVols != null && File.Exists(settings.DernierCatalogueVols))
            volsPath = settings.DernierCatalogueVols;
        if (equipPath == null && settings.DernierEquipage != null && File.Exists(settings.DernierEquipage))
            equipPath = settings.DernierEquipage;
    }

    var config = new Configuration();

    // Legacy: single file loads everything
    if (configPath != null)
    {
        config = XmlConfigLoader.Charger(configPath);
        paramPath = configPath;
    }
    else
    {
        // Load each file type and merge
        if (paramPath != null)
            XmlConfigLoader.MergerParametres(config, XmlConfigLoader.ChargerParametres(paramPath));
        if (progPath != null)
            XmlConfigLoader.MergerProgramme(config, XmlConfigLoader.ChargerProgramme(progPath));
        if (volsPath != null)
            XmlConfigLoader.MergerCatalogueVols(config, XmlConfigLoader.ChargerCatalogueVols(volsPath));
        if (equipPath != null)
            XmlConfigLoader.MergerEquipage(config, XmlConfigLoader.ChargerEquipage(equipPath));
    }

    ITheme theme = style switch
    {
        "minimalist" or "min" => new MinimalistTheme(),
        _ => new BorlandTheme()
    };

    var app = new TuiApp(config, theme, paramPath, progPath, volsPath, equipPath, settings);
    app.Run();
    return 0;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Erreur: {ex.Message}");
    return 10;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Erreur: {ex.Message}");
    return 99;
}

// ── Fonctions ──

static int RunBatch(string configPath, bool quiet)
{
    try
    {
        var config = XmlConfigLoader.Charger(configPath);

        var resultat = CalculateurMarge.Calculer(config);

        if (quiet)
        {
            Console.WriteLine($"{resultat.TauxEngagementGlobal:P1} {resultat.StatutGlobal}");
        }
        else
        {
            ConsoleRenderer.Afficher(resultat, config);
        }

        return resultat.StatutGlobal switch
        {
            "CONFORTABLE" => 0,
            "TENDU" => 1,
            "CRITIQUE" => 2,
            "ERREUR" => 11,
            _ => 0
        };
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine($"Erreur: {ex.Message}");
        return 10;
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine($"Erreur de parametrage: {ex.Message}");
        return 11;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Erreur inattendue: {ex.Message}");
        return 99;
    }
}

static void AfficherAide()
{
    Console.WriteLine("""
        CrewSizer -- Calcul de marge d'engagement equipage

        Usage: CrewSizer [fichiers...] [options]

        Fichiers (auto-detection du type XML):
          Parametres.xml          Fichier <Parametres>
          Programme.xml           Fichier <Programme>
          CatalogueVols.xml       Fichier <CatalogueVols>
          Equipage.xml            Fichier <Equipage>
          config.xml              Fichier legacy <Configuration>

        Options nommees:
          --param <path>          Fichier parametres
          --prog <path>           Fichier programme
          --vols <path>           Fichier catalogue vols
          --equip <path>          Fichier equipage

        Modes:
          (sans args)             Mode TUI interactif (defaut)
          -c, --config <path>     Mode batch avec fichier legacy
          -q, --quiet             Mode batch, sortie minimale
          --batch                 Forcer le mode batch
          --tui                   Forcer le mode TUI

        Options:
          --srcdir <path>         Definir le repertoire de sources
          --style borland|min     Theme TUI (defaut: borland)
          --help                  Afficher cette aide
        """);
}
