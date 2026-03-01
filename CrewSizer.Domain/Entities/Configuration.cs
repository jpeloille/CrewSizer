using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Domain.Entities;

/// <summary>
/// Objet de transfert pour le moteur de calcul CalculateurMarge.
/// Reconstruit depuis ConfigurationScenario + catalogues.
/// </summary>
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

    /// <summary>
    /// Reconstruit une Configuration depuis un ConfigurationScenario + catalogues.
    /// Adaptateur clé pour le CalculateurMarge inchangé.
    /// </summary>
    public static Configuration FromScenario(
        ConfigurationScenario scenario,
        List<Vol> catalogueVols,
        List<BlocVol> catalogueBlocs,
        List<SemaineType> semainesTypes,
        DonneesEquipage? equipage = null)
    {
        // Effectif : opérationnel (compétents + disponibles) si import APM, sinon fallback scénario
        var effectif = equipage != null
            ? equipage.CalculerEffectifOperationnel(
                scenario.Periode.DateDebut.ToDateTime(TimeOnly.MinValue),
                scenario.Periode.DateFin.ToDateTime(TimeOnly.MinValue))
            : scenario.Effectif;

        return new Configuration
        {
            Periode = scenario.Periode,
            Effectif = effectif,
            LimitesFTL = scenario.LimitesFTL,
            LimitesCumulatives = scenario.LimitesCumulatives,
            JoursOff = scenario.JoursOff,
            LimitesTempsService = scenario.LimitesTempsService,
            FonctionsSolPNT = scenario.FonctionsSolPNT,
            FonctionsSolPNC = scenario.FonctionsSolPNC,
            AbattementsPNT = scenario.AbattementsPNT,
            AbattementsPNC = scenario.AbattementsPNC,
            TableTsvMax = scenario.TableTsvMax,
            Calendrier = scenario.Calendrier,
            CatalogueVols = catalogueVols,
            CatalogueBlocs = catalogueBlocs,
            SemainesTypes = semainesTypes,
            Equipage = equipage
        };
    }
}
