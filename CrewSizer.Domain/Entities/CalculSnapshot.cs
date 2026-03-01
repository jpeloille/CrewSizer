namespace CrewSizer.Domain.Entities;

/// <summary>
/// Résultat de calcul persisté (lecture seule après création).
/// Permet l'historique et la comparaison de scénarios.
/// </summary>
public class CalculSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScenarioId { get; set; }
    public DateTime DateCalcul { get; set; } = DateTime.UtcNow;
    public string? CalculePar { get; set; }

    // Résultats clés (dénormalisés pour requêtes rapides)
    public double TauxEngagementGlobal { get; set; }
    public string StatutGlobal { get; set; } = "";
    public string CategorieContraignante { get; set; } = "";
    public int TotalBlocs { get; set; }
    public double TotalHDV { get; set; }
    public int Rotations { get; set; }

    // Résultat complet sérialisé en JSONB
    public string ResultatJson { get; set; } = "";

    // Navigation
    public ConfigurationScenario? Scenario { get; set; }
}
