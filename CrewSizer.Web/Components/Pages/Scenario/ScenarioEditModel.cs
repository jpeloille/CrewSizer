using CrewSizer.Application.Common.Dtos;

namespace CrewSizer.Web.Components.Pages.Scenario;

/// <summary>
/// Modèle mutable pour le binding bidirectionnel DxFormLayout.
/// ScenarioDto est un record immutable (init) — on a besoin de set.
/// </summary>
public class ScenarioEditModel
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Description { get; set; }

    // Période
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }

    // Effectif
    public int Cdb { get; set; }
    public int Opl { get; set; }
    public int Cc { get; set; }
    public int Pnc { get; set; }

    // Limites FTL
    public double TsvMaxJournalier { get; set; } = 13.0;
    public double TsvMoyenRetenu { get; set; } = 10.0;
    public double ReposMinimum { get; set; } = 12.0;

    // Limites cumulatives
    public double H28Max { get; set; } = 100;
    public double H90Max { get; set; } = 280;
    public double H12Max { get; set; } = 900;
    public double CumulPntCumul28 { get; set; }
    public double CumulPntCumul90 { get; set; }
    public double CumulPntCumul12 { get; set; }
    public double CumulPncCumul28 { get; set; }
    public double CumulPncCumul90 { get; set; }
    public double CumulPncCumul12 { get; set; }

    // Jours OFF
    public int OffReglementaire { get; set; } = 8;
    public int OffAccordEntreprise { get; set; } = 2;

    // Limites temps de service
    public double TsMax7j { get; set; } = 60;
    public double TsMax14j { get; set; } = 110;
    public double TsMax28j { get; set; } = 190;

    // Collections
    public List<FonctionSolDto> FonctionsSolPNT { get; set; } = [];
    public List<FonctionSolDto> FonctionsSolPNC { get; set; } = [];
    public List<AbattementDto> AbattementsPNT { get; set; } = [];
    public List<AbattementDto> AbattementsPNC { get; set; } = [];
    public List<EntreeTsvMaxDto> TableTsvMax { get; set; } = [];
    public List<AffectationSemaineDto> Calendrier { get; set; } = [];

    public uint Version { get; set; }

    public static ScenarioEditModel FromDto(ScenarioDto dto) => new()
    {
        Id = dto.Id,
        Nom = dto.Nom,
        Description = dto.Description,
        DateDebut = dto.DateDebut,
        DateFin = dto.DateFin,
        Cdb = dto.Cdb,
        Opl = dto.Opl,
        Cc = dto.Cc,
        Pnc = dto.Pnc,
        TsvMaxJournalier = dto.TsvMaxJournalier,
        TsvMoyenRetenu = dto.TsvMoyenRetenu,
        ReposMinimum = dto.ReposMinimum,
        H28Max = dto.H28Max,
        H90Max = dto.H90Max,
        H12Max = dto.H12Max,
        CumulPntCumul28 = dto.CumulPntCumul28,
        CumulPntCumul90 = dto.CumulPntCumul90,
        CumulPntCumul12 = dto.CumulPntCumul12,
        CumulPncCumul28 = dto.CumulPncCumul28,
        CumulPncCumul90 = dto.CumulPncCumul90,
        CumulPncCumul12 = dto.CumulPncCumul12,
        OffReglementaire = dto.OffReglementaire,
        OffAccordEntreprise = dto.OffAccordEntreprise,
        TsMax7j = dto.TsMax7j,
        TsMax14j = dto.TsMax14j,
        TsMax28j = dto.TsMax28j,
        FonctionsSolPNT = dto.FonctionsSolPNT.ToList(),
        FonctionsSolPNC = dto.FonctionsSolPNC.ToList(),
        AbattementsPNT = dto.AbattementsPNT.ToList(),
        AbattementsPNC = dto.AbattementsPNC.ToList(),
        TableTsvMax = dto.TableTsvMax.ToList(),
        Calendrier = dto.Calendrier.ToList(),
        Version = dto.Version
    };

    public ScenarioDto ToDto() => new()
    {
        Id = Id,
        Nom = Nom,
        Description = Description,
        DateDebut = DateDebut,
        DateFin = DateFin,
        Cdb = Cdb,
        Opl = Opl,
        Cc = Cc,
        Pnc = Pnc,
        TsvMaxJournalier = TsvMaxJournalier,
        TsvMoyenRetenu = TsvMoyenRetenu,
        ReposMinimum = ReposMinimum,
        H28Max = H28Max,
        H90Max = H90Max,
        H12Max = H12Max,
        CumulPntCumul28 = CumulPntCumul28,
        CumulPntCumul90 = CumulPntCumul90,
        CumulPntCumul12 = CumulPntCumul12,
        CumulPncCumul28 = CumulPncCumul28,
        CumulPncCumul90 = CumulPncCumul90,
        CumulPncCumul12 = CumulPncCumul12,
        OffReglementaire = OffReglementaire,
        OffAccordEntreprise = OffAccordEntreprise,
        TsMax7j = TsMax7j,
        TsMax14j = TsMax14j,
        TsMax28j = TsMax28j,
        FonctionsSolPNT = FonctionsSolPNT,
        FonctionsSolPNC = FonctionsSolPNC,
        AbattementsPNT = AbattementsPNT,
        AbattementsPNC = AbattementsPNC,
        TableTsvMax = TableTsvMax,
        Calendrier = Calendrier,
        Version = Version
    };
}
