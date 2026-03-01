using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Définition d'un type de check/qualification</summary>
public class DefinitionCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Primaire { get; set; }
    public GroupeCheck Groupe { get; set; }
    public int ValiditeNombre { get; set; }
    public string ValiditeUnite { get; set; } = "";
    public bool FinDeMois { get; set; }
    public bool FinDAnnee { get; set; }
    public int RenouvellementNombre { get; set; }
    public string RenouvellementUnite { get; set; } = "";
    public int AvertissementNombre { get; set; }
    public string AvertissementUnite { get; set; } = "";
}
