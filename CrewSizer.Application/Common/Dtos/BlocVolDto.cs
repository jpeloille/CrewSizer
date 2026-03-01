namespace CrewSizer.Application.Common.Dtos;

public record BlocVolDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public int Sequence { get; init; }
    public string Jour { get; init; } = "";
    public string Periode { get; init; } = "";
    public string DebutDP { get; init; } = "";
    public string FinDP { get; init; } = "";
    public string DebutFDP { get; init; } = "";
    public string FinFDP { get; init; } = "";
    public List<EtapeVolDto> Etapes { get; init; } = [];

    // BlocType (nullable)
    public Guid? BlocTypeId { get; init; }
    public string? BlocTypeCode { get; init; }
    public string? BlocTypeLibelle { get; init; }

    // TypeAvion (obligatoire)
    public Guid TypeAvionId { get; init; }
    public string? TypeAvionCode { get; init; }
    public string? TypeAvionLibelle { get; init; }

    // Propriétés calculées (lecture seule, remplies par le mapping)
    public string Nom { get; init; } = "";
    public int NbEtapes { get; init; }
    public double HdvBloc { get; init; }
    public double DureeTSVHeures { get; init; }
}

public record EtapeVolDto(int Position, Guid VolId, int? Modificateur = null);
