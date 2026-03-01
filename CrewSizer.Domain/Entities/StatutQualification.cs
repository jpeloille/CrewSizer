using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Statut d'une qualification pour un membre</summary>
public class StatutQualification
{
    public string CodeCheck { get; set; } = "";
    public DateTime? DateExpiration { get; set; }
    public StatutCheck Statut { get; set; } = StatutCheck.NonApplicable;
}
