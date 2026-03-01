using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Domain.Entities;

/// <summary>
/// Scénario de calcul : encapsule un jeu complet de paramètres.
/// Racine d'agrégat principal.
/// </summary>
public class ConfigurationScenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
    public string? CreePar { get; set; }
    public string? ModifiePar { get; set; }

    // Value objects (inline via OwnsOne)
    public Periode Periode { get; set; } = new();
    public Effectif Effectif { get; set; } = new();
    public LimitesFTL LimitesFTL { get; set; } = new();
    public LimitesCumulatives LimitesCumulatives { get; set; } = new();
    public JoursOff JoursOff { get; set; } = new();
    public LimitesTempsService LimitesTempsService { get; set; } = new();

    // Collections (JSONB)
    public List<FonctionSol> FonctionsSolPNT { get; set; } = [];
    public List<FonctionSol> FonctionsSolPNC { get; set; } = [];
    public List<Abattement> AbattementsPNT { get; set; } = [];
    public List<Abattement> AbattementsPNC { get; set; } = [];
    public List<EntreeTsvMax> TableTsvMax { get; set; } = [];

    // Navigation
    public List<AffectationSemaine> Calendrier { get; set; } = [];

    // Concurrence optimiste (PostgreSQL xmin)
    public uint Version { get; set; }
}
