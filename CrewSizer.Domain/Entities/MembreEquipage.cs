using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Membre d'équipage (PNT ou PNC)</summary>
public class MembreEquipage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Nom { get; set; } = "";
    public bool Actif { get; set; } = true;
    public TypeContrat Contrat { get; set; }
    public Grade Grade { get; set; }
    public string Matricule { get; set; } = "";
    public DateTime? DateEntree { get; set; }
    public DateTime? DateFin { get; set; }
    public List<string> Roles { get; set; } = [];
    public string Categorie { get; set; } = "";
    public List<string> ReglesApplicables { get; set; } = [];
    public List<string> Bases { get; set; } = [];
    public string TypeAvion { get; set; } = "";
    public List<StatutQualification> Qualifications { get; set; } = [];
}
