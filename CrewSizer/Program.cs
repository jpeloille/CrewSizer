using CrewSizer.IO;
using CrewSizer.Models;
using CrewSizer.Services;
using CrewSizer.Tui;

// ── Parsing arguments ──
string? configPath = null;
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
        case "--help":
            AfficherAide();
            return 0;
        default:
            // Argument positionnel = fichier de config (pour le TUI)
            if (!args[i].StartsWith('-'))
                configPath = args[i];
            break;
    }
}

// ── Mode batch ──
if (batch)
{
    return RunBatch(configPath ?? Path.Combine(AppContext.BaseDirectory, "Config", "parametres.xml"), quiet);
}

// ── Mode TUI ──
try
{
    Configuration config;
    if (configPath != null)
    {
        config = XmlConfigLoader.Charger(configPath);
    }
    else
    {
        config = new Configuration();
    }

    ITheme theme = style switch
    {
        "minimalist" or "min" => new MinimalistTheme(),
        _ => new BorlandTheme()
    };

    var app = new TuiApp(config, theme, configPath);
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

        Usage: CrewSizer [options]

        Modes:
          (sans args)             Mode TUI interactif (defaut)
          -c, --config <path>     Mode batch avec fichier de config
          -q, --quiet             Mode batch, sortie minimale
          --batch                 Forcer le mode batch
          --tui                   Forcer le mode TUI

        Options:
          --style borland|min     Theme TUI (defaut: borland)
          --help                  Afficher cette aide

        Format: .xml
        """);
}
