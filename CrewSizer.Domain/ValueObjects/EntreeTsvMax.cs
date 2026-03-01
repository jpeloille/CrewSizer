namespace CrewSizer.Domain.ValueObjects;

/// <summary>Entrée de la table TSV max EU-OPS</summary>
public class EntreeTsvMax
{
    /// <summary>Début de la bande horaire (inclusive), format "HH:mm"</summary>
    public string DebutBande { get; set; } = "";

    /// <summary>Fin de la bande horaire (exclusive), format "HH:mm"</summary>
    public string FinBande { get; set; } = "";

    /// <summary>TSV max en heures, indexé par nombre d'étapes. Clé = nb étapes, Valeur = max TSV heures.</summary>
    public Dictionary<int, double> MaxParEtapes { get; set; } = new();
}
