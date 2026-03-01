using CrewSizer.Domain.Enums;

namespace CrewSizer.Application.Common.Dtos;

public record StatutQualificationDto
{
    public string CodeCheck { get; init; } = "";
    public DateTime? DateExpiration { get; init; }
    public StatutCheck Statut { get; init; }
}

public record MembreDetailDto
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
    public List<StatutQualificationDto> Qualifications { get; init; } = [];
    public int NbChecksValides { get; init; }
    public int NbChecksExpires { get; init; }
    public int NbChecksAvertissement { get; init; }
    public StatutCheck StatutGlobal { get; init; }
}

public record DefinitionCheckDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Primaire { get; init; }
    public GroupeCheck Groupe { get; init; }
    public int ValiditeNombre { get; init; }
    public string ValiditeUnite { get; init; } = "";
    public bool FinDeMois { get; init; }
    public bool FinDAnnee { get; init; }
    public int RenouvellementNombre { get; init; }
    public string RenouvellementUnite { get; init; } = "";
    public int AvertissementNombre { get; init; }
    public string AvertissementUnite { get; init; } = "";
    public int NbMembresValides { get; init; }
    public int NbMembresExpires { get; init; }
    public int NbMembresAvertissement { get; init; }
    public int NbMembresTotal { get; init; }
}

public record MatriceCellDto
{
    public string CodeCheck { get; init; } = "";
    public StatutCheck Statut { get; init; }
    public DateTime? DateExpiration { get; init; }
}

public record MatriceLigneDto
{
    public Guid MembreId { get; init; }
    public string Code { get; init; } = "";
    public string Nom { get; init; } = "";
    public Grade Grade { get; init; }
    public TypeContrat Contrat { get; init; }
    public Dictionary<string, MatriceCellDto> Checks { get; init; } = new();
}

public record MatriceQualificationsDto
{
    public List<string> CodesChecks { get; init; } = [];
    public List<MatriceLigneDto> Lignes { get; init; } = [];
    public GroupeCheck? FiltreGroupe { get; init; }
}

public record AlerteQualificationDto
{
    public Guid MembreId { get; init; }
    public string MembreCode { get; init; } = "";
    public string MembreNom { get; init; } = "";
    public Grade Grade { get; init; }
    public string CodeCheck { get; init; } = "";
    public string DescriptionCheck { get; init; } = "";
    public StatutCheck Statut { get; init; }
    public DateTime? DateExpiration { get; init; }
    public int? JoursRestants { get; init; }
}

public record EquipageKpiDto
{
    public int TotalMembres { get; init; }
    public int TotalActifs { get; init; }
    public int Cdb { get; init; }
    public int Opl { get; init; }
    public int Cc { get; init; }
    public int Pnc { get; init; }
    public int AlertesExpirees { get; init; }
    public int AlertesProches { get; init; }
    public int AlertesAvertissement { get; init; }
}
