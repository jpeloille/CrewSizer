using CrewSizer.Domain.Enums;

namespace CrewSizer.Application.Common.Dtos;

public record MembreEquipageDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Nom { get; init; } = "";
    public bool Actif { get; init; }
    public TypeContrat Contrat { get; init; }
    public Grade Grade { get; init; }
    public string Matricule { get; init; } = "";
    public DateTime? DateEntree { get; init; }
    public DateTime? DateFin { get; init; }
    public List<string> Roles { get; init; } = [];
    public string Categorie { get; init; } = "";
    public string TypeAvion { get; init; } = "";
    public List<string> Bases { get; init; } = [];
    public StatutCheck StatutGlobal { get; init; } = StatutCheck.NonApplicable;
    public List<StatutCheck> QualificationsResume { get; init; } = [];
}

public record QualificationMatrixDto
{
    public int TotalMembres { get; init; }
    public int Cdb { get; init; }
    public int Opl { get; init; }
    public int Cc { get; init; }
    public int Pnc { get; init; }
    public DateTime? DateExtraction { get; init; }
    public List<MembreEquipageDto> Membres { get; init; } = [];
}

public record ImportEquipageResultDto
{
    public int NbMembresImportes { get; init; }
    public int NbChecksImportes { get; init; }
    public DateTime DateExtraction { get; init; }
    public List<string> Avertissements { get; init; } = [];
}
