using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrewSizer.IO;

public class AppSettings
{
    public string? RepertoireSources { get; set; }
    public string? DernierParametres { get; set; }
    public string? DernierProgramme { get; set; }
    public string? DernierCatalogueVols { get; set; }
    public string? DernierEquipage { get; set; }

    [JsonIgnore]
    public string RepertoireEffectif =>
        !string.IsNullOrWhiteSpace(RepertoireSources) && Directory.Exists(RepertoireSources)
            ? RepertoireSources
            : Environment.CurrentDirectory;

    // ── Persistance ──

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string CheminFichier =>
        Path.Combine(AppContext.BaseDirectory, "crewsizer-settings.json");

    public static AppSettings Charger()
    {
        var path = CheminFichier;
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Sauvegarder()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(CheminFichier, json);
        }
        catch
        {
            // Silencieux si le répertoire est en lecture seule
        }
    }
}
