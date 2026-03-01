namespace CrewSizer.Application.Common.Dtos;

public record ScenarioDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = "";
    public string? Description { get; init; }
    public DateTime DateCreation { get; init; }
    public DateTime DateModification { get; init; }
    public string? CreePar { get; init; }
    public string? ModifiePar { get; init; }

    // Période
    public DateOnly DateDebut { get; init; }
    public DateOnly DateFin { get; init; }

    // Effectif
    public int Cdb { get; init; }
    public int Opl { get; init; }
    public int Cc { get; init; }
    public int Pnc { get; init; }

    // Limites FTL
    public double TsvMaxJournalier { get; init; } = 13.0;
    public double TsvMoyenRetenu { get; init; } = 10.0;
    public double ReposMinimum { get; init; } = 12.0;

    // Limites cumulatives
    public double H28Max { get; init; } = 100;
    public double H90Max { get; init; } = 280;
    public double H12Max { get; init; } = 900;
    public double CumulPntCumul28 { get; init; }
    public double CumulPntCumul90 { get; init; }
    public double CumulPntCumul12 { get; init; }
    public double CumulPncCumul28 { get; init; }
    public double CumulPncCumul90 { get; init; }
    public double CumulPncCumul12 { get; init; }

    // Jours OFF
    public int OffReglementaire { get; init; } = 8;
    public int OffAccordEntreprise { get; init; } = 2;

    // Limites temps de service
    public double TsMax7j { get; init; } = 60;
    public double TsMax14j { get; init; } = 110;
    public double TsMax28j { get; init; } = 190;

    // Collections JSONB
    public List<FonctionSolDto> FonctionsSolPNT { get; init; } = [];
    public List<FonctionSolDto> FonctionsSolPNC { get; init; } = [];
    public List<AbattementDto> AbattementsPNT { get; init; } = [];
    public List<AbattementDto> AbattementsPNC { get; init; } = [];
    public List<EntreeTsvMaxDto> TableTsvMax { get; init; } = [];

    // Calendrier
    public List<AffectationSemaineDto> Calendrier { get; init; } = [];

    public uint Version { get; init; }
}

public record ScenarioListItemDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = "";
    public string? Description { get; init; }
    public DateTime DateModification { get; init; }
    public string? ModifiePar { get; init; }
    public DateOnly DateDebut { get; init; }
    public DateOnly DateFin { get; init; }
}

public record FonctionSolDto(string Nom, int NbPersonnes, int JoursSolMois);
public record AbattementDto(string Libelle, int JoursPersonnel);
public record EntreeTsvMaxDto(string DebutBande, string FinBande, Dictionary<int, double> MaxParEtapes);
public record AffectationSemaineDto(int Semaine, int Annee, Guid SemaineTypeId, string SemaineTypeRef);
