using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Entities;
using CrewSizer.Domain.Enums;
using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Application.Common.Mappings;

public static class DtoMappingExtensions
{
    // ── Vol ──

    public static VolDto ToDto(this Vol vol) => new()
    {
        Id = vol.Id,
        Numero = vol.Numero,
        Depart = vol.Depart,
        Arrivee = vol.Arrivee,
        HeureDepart = vol.HeureDepart,
        HeureArrivee = vol.HeureArrivee,
        MH = vol.MH,
        HdvVol = vol.HdvVol
    };

    public static Vol ToEntity(this VolDto dto) => new()
    {
        Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
        Numero = dto.Numero,
        Depart = dto.Depart,
        Arrivee = dto.Arrivee,
        HeureDepart = dto.HeureDepart,
        HeureArrivee = dto.HeureArrivee,
        MH = dto.MH
    };

    // ── BlocType ──

    public static BlocTypeDto ToDto(this BlocType bt) => new()
    {
        Id = bt.Id,
        Code = bt.Code,
        Libelle = bt.Libelle,
        DebutPlage = bt.DebutPlage,
        FinPlage = bt.FinPlage,
        FdpMax = bt.FdpMax,
        HauteSaison = bt.HauteSaison
    };

    public static BlocType ToEntity(this BlocTypeDto dto) => new()
    {
        Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
        Code = dto.Code,
        Libelle = dto.Libelle,
        DebutPlage = dto.DebutPlage,
        FinPlage = dto.FinPlage,
        FdpMax = dto.FdpMax,
        HauteSaison = dto.HauteSaison
    };

    // ── TypeAvion ──

    public static TypeAvionDto ToDto(this TypeAvion ta) => new()
    {
        Id = ta.Id,
        Code = ta.Code,
        Libelle = ta.Libelle,
        NbCdb = ta.NbCdb,
        NbOpl = ta.NbOpl,
        NbCc = ta.NbCc,
        NbPnc = ta.NbPnc
    };

    public static TypeAvion ToEntity(this TypeAvionDto dto) => new()
    {
        Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
        Code = dto.Code,
        Libelle = dto.Libelle,
        NbCdb = dto.NbCdb,
        NbOpl = dto.NbOpl,
        NbCc = dto.NbCc,
        NbPnc = dto.NbPnc
    };

    // ── BlocVol ──

    public static BlocVolDto ToDto(this BlocVol bloc) => new()
    {
        Id = bloc.Id,
        Code = bloc.Code,
        Sequence = bloc.Sequence,
        Jour = bloc.Jour,
        Periode = bloc.Periode,
        DebutDP = bloc.DebutDP,
        FinDP = bloc.FinDP,
        DebutFDP = bloc.DebutFDP,
        FinFDP = bloc.FinFDP,
        Etapes = bloc.Etapes.Select(e => new EtapeVolDto(e.Position, e.VolId, e.Modificateur)).ToList(),
        BlocTypeId = bloc.BlocTypeId,
        BlocTypeCode = bloc.BlocType?.Code,
        BlocTypeLibelle = bloc.BlocType?.Libelle,
        TypeAvionId = bloc.TypeAvionId,
        TypeAvionCode = bloc.TypeAvion?.Code,
        TypeAvionLibelle = bloc.TypeAvion?.Libelle,
        Nom = bloc.Nom,
        NbEtapes = bloc.NbEtapes,
        HdvBloc = bloc.HdvBloc,
        DureeTSVHeures = bloc.DureeTSVHeures
    };

    public static BlocVol ToEntity(this BlocVolDto dto) => new()
    {
        Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
        Code = dto.Code,
        Sequence = dto.Sequence,
        Jour = dto.Jour,
        Periode = dto.Periode,
        DebutDP = dto.DebutDP,
        FinDP = dto.FinDP,
        DebutFDP = dto.DebutFDP,
        FinFDP = dto.FinFDP,
        BlocTypeId = dto.BlocTypeId,
        TypeAvionId = dto.TypeAvionId,
        Etapes = dto.Etapes.Select(e => new EtapeVol { Position = e.Position, VolId = e.VolId, Modificateur = e.Modificateur }).ToList()
    };

    // ── SemaineType ──

    public static SemaineTypeDto ToDto(this SemaineType st) => new()
    {
        Id = st.Id,
        Reference = st.Reference,
        Saison = st.Saison,
        Placements = st.Placements.Select(p => new BlocPlacementDto(p.BlocId, p.Jour, p.Sequence)).ToList()
    };

    public static SemaineType ToEntity(this SemaineTypeDto dto) => new()
    {
        Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
        Reference = dto.Reference,
        Saison = dto.Saison,
        Placements = dto.Placements.Select(p => new BlocPlacement
        {
            BlocId = p.BlocId,
            Jour = p.Jour,
            Sequence = p.Sequence
        }).ToList()
    };

    // ── ConfigurationScenario ──

    public static ScenarioDto ToDto(this ConfigurationScenario s) => new()
    {
        Id = s.Id,
        Nom = s.Nom,
        Description = s.Description,
        DateCreation = s.DateCreation,
        DateModification = s.DateModification,
        CreePar = s.CreePar,
        ModifiePar = s.ModifiePar,
        DateDebut = s.Periode.DateDebut,
        DateFin = s.Periode.DateFin,
        Cdb = s.Effectif.Cdb,
        Opl = s.Effectif.Opl,
        Cc = s.Effectif.Cc,
        Pnc = s.Effectif.Pnc,
        TsvMaxJournalier = s.LimitesFTL.TsvMaxJournalier,
        TsvMoyenRetenu = s.LimitesFTL.TsvMoyenRetenu,
        ReposMinimum = s.LimitesFTL.ReposMinimum,
        H28Max = s.LimitesCumulatives.H28Max,
        H90Max = s.LimitesCumulatives.H90Max,
        H12Max = s.LimitesCumulatives.H12Max,
        CumulPntCumul28 = s.LimitesCumulatives.CumulPNT.Cumul28Entrant,
        CumulPntCumul90 = s.LimitesCumulatives.CumulPNT.Cumul90Entrant,
        CumulPntCumul12 = s.LimitesCumulatives.CumulPNT.Cumul12Entrant,
        CumulPncCumul28 = s.LimitesCumulatives.CumulPNC.Cumul28Entrant,
        CumulPncCumul90 = s.LimitesCumulatives.CumulPNC.Cumul90Entrant,
        CumulPncCumul12 = s.LimitesCumulatives.CumulPNC.Cumul12Entrant,
        OffReglementaire = s.JoursOff.Reglementaire,
        OffAccordEntreprise = s.JoursOff.AccordEntreprise,
        TsMax7j = s.LimitesTempsService.Max7j,
        TsMax14j = s.LimitesTempsService.Max14j,
        TsMax28j = s.LimitesTempsService.Max28j,
        FonctionsSolPNT = s.FonctionsSolPNT.Select(f => new FonctionSolDto(f.Nom, f.NbPersonnes, f.JoursSolMois)).ToList(),
        FonctionsSolPNC = s.FonctionsSolPNC.Select(f => new FonctionSolDto(f.Nom, f.NbPersonnes, f.JoursSolMois)).ToList(),
        AbattementsPNT = s.AbattementsPNT.Select(a => new AbattementDto(a.Libelle, a.JoursPersonnel)).ToList(),
        AbattementsPNC = s.AbattementsPNC.Select(a => new AbattementDto(a.Libelle, a.JoursPersonnel)).ToList(),
        TableTsvMax = s.TableTsvMax.Select(t => new EntreeTsvMaxDto(t.DebutBande, t.FinBande, t.MaxParEtapes)).ToList(),
        Calendrier = s.Calendrier.Select(c => new AffectationSemaineDto(c.Semaine, c.Annee, c.SemaineTypeId, c.SemaineTypeRef)).ToList(),
        Version = s.Version
    };

    public static ScenarioListItemDto ToListItemDto(this ConfigurationScenario s) => new()
    {
        Id = s.Id,
        Nom = s.Nom,
        Description = s.Description,
        DateModification = s.DateModification,
        ModifiePar = s.ModifiePar,
        DateDebut = s.Periode.DateDebut,
        DateFin = s.Periode.DateFin
    };

    public static void ApplyToEntity(this ScenarioDto dto, ConfigurationScenario entity)
    {
        entity.Nom = dto.Nom;
        entity.Description = dto.Description;
        entity.ModifiePar = dto.ModifiePar;
        entity.DateModification = DateTime.UtcNow;

        entity.Periode = new Periode { DateDebut = dto.DateDebut, DateFin = dto.DateFin };
        entity.Effectif = new Effectif { Cdb = dto.Cdb, Opl = dto.Opl, Cc = dto.Cc, Pnc = dto.Pnc };
        entity.LimitesFTL = new LimitesFTL
        {
            TsvMaxJournalier = dto.TsvMaxJournalier,
            TsvMoyenRetenu = dto.TsvMoyenRetenu,
            ReposMinimum = dto.ReposMinimum
        };
        entity.LimitesCumulatives = new LimitesCumulatives
        {
            H28Max = dto.H28Max,
            H90Max = dto.H90Max,
            H12Max = dto.H12Max,
            CumulPNT = new CumulEntrant
            {
                Cumul28Entrant = dto.CumulPntCumul28,
                Cumul90Entrant = dto.CumulPntCumul90,
                Cumul12Entrant = dto.CumulPntCumul12
            },
            CumulPNC = new CumulEntrant
            {
                Cumul28Entrant = dto.CumulPncCumul28,
                Cumul90Entrant = dto.CumulPncCumul90,
                Cumul12Entrant = dto.CumulPncCumul12
            }
        };
        entity.JoursOff = new JoursOff
        {
            Reglementaire = dto.OffReglementaire,
            AccordEntreprise = dto.OffAccordEntreprise
        };
        entity.LimitesTempsService = new LimitesTempsService
        {
            Max7j = dto.TsMax7j,
            Max14j = dto.TsMax14j,
            Max28j = dto.TsMax28j
        };
        entity.FonctionsSolPNT = dto.FonctionsSolPNT.Select(f => new FonctionSol
        {
            Nom = f.Nom, NbPersonnes = f.NbPersonnes, JoursSolMois = f.JoursSolMois
        }).ToList();
        entity.FonctionsSolPNC = dto.FonctionsSolPNC.Select(f => new FonctionSol
        {
            Nom = f.Nom, NbPersonnes = f.NbPersonnes, JoursSolMois = f.JoursSolMois
        }).ToList();
        entity.AbattementsPNT = dto.AbattementsPNT.Select(a => new Abattement
        {
            Libelle = a.Libelle, JoursPersonnel = a.JoursPersonnel
        }).ToList();
        entity.AbattementsPNC = dto.AbattementsPNC.Select(a => new Abattement
        {
            Libelle = a.Libelle, JoursPersonnel = a.JoursPersonnel
        }).ToList();
        entity.TableTsvMax = dto.TableTsvMax.Select(t => new EntreeTsvMax
        {
            DebutBande = t.DebutBande, FinBande = t.FinBande, MaxParEtapes = t.MaxParEtapes
        }).ToList();
        entity.Calendrier = dto.Calendrier.Select(c => new AffectationSemaine
        {
            Semaine = c.Semaine, Annee = c.Annee, SemaineTypeId = c.SemaineTypeId, SemaineTypeRef = c.SemaineTypeRef
        }).ToList();
    }

    // ── CalculSnapshot ──

    public static CalculSnapshotDto ToDto(this CalculSnapshot snap) => new()
    {
        Id = snap.Id,
        ScenarioId = snap.ScenarioId,
        DateCalcul = snap.DateCalcul,
        CalculePar = snap.CalculePar,
        TauxEngagementGlobal = snap.TauxEngagementGlobal,
        StatutGlobal = snap.StatutGlobal,
        CategorieContraignante = snap.CategorieContraignante,
        TotalBlocs = snap.TotalBlocs,
        TotalHDV = snap.TotalHDV,
        Rotations = snap.Rotations,
        ResultatJson = snap.ResultatJson
    };

    public static CalculSnapshotListItemDto ToListItemDto(this CalculSnapshot snap) => new()
    {
        Id = snap.Id,
        ScenarioId = snap.ScenarioId,
        DateCalcul = snap.DateCalcul,
        CalculePar = snap.CalculePar,
        TauxEngagementGlobal = snap.TauxEngagementGlobal,
        StatutGlobal = snap.StatutGlobal,
        CategorieContraignante = snap.CategorieContraignante
    };

    // ── MembreEquipage ──

    public static MembreEquipageDto ToDto(this MembreEquipage m) => new()
    {
        Id = m.Id,
        Code = m.Code,
        Nom = m.Nom,
        Actif = m.Actif,
        Contrat = m.Contrat,
        Grade = m.Grade,
        Matricule = m.Matricule,
        DateEntree = m.DateEntree,
        DateFin = m.DateFin,
        Roles = m.Roles,
        Categorie = m.Categorie,
        TypeAvion = m.TypeAvion,
        Bases = m.Bases,
        StatutGlobal = m.Qualifications.Count == 0
            ? StatutCheck.NonApplicable
            : m.Qualifications.Max(q => q.Statut),
        QualificationsResume = m.Qualifications.Select(q => q.Statut).ToList()
    };

    // ── StatutQualification ──

    public static StatutQualificationDto ToDto(this StatutQualification q) => new()
    {
        CodeCheck = q.CodeCheck,
        DateExpiration = q.DateExpiration,
        Statut = q.Statut
    };

    // ── MembreEquipage → détail avec qualifications ──

    public static MembreDetailDto ToDetailDto(this MembreEquipage m) => new()
    {
        Id = m.Id,
        Code = m.Code,
        Nom = m.Nom,
        Actif = m.Actif,
        Contrat = m.Contrat,
        Grade = m.Grade,
        Matricule = m.Matricule,
        DateEntree = m.DateEntree,
        DateFin = m.DateFin,
        Roles = m.Roles,
        Categorie = m.Categorie,
        TypeAvion = m.TypeAvion,
        Bases = m.Bases,
        Qualifications = m.Qualifications.Select(q => q.ToDto()).ToList(),
        NbChecksValides = m.Qualifications.Count(q => q.Statut == StatutCheck.Valide),
        NbChecksExpires = m.Qualifications.Count(q => q.Statut == StatutCheck.Expire),
        NbChecksAvertissement = m.Qualifications.Count(q =>
            q.Statut is StatutCheck.Avertissement or StatutCheck.ExpirationProche),
        StatutGlobal = m.Qualifications.Count == 0
            ? StatutCheck.NonApplicable
            : m.Qualifications.Max(q => q.Statut)
    };

    // ── DefinitionCheck (existant — via JSONB Qualifications) ──

    public static DefinitionCheckDto ToDto(this DefinitionCheck d, List<MembreEquipage> membres) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Description = d.Description,
        Primaire = d.Primaire,
        Groupe = d.Groupe,
        ValiditeNombre = d.ValiditeNombre,
        ValiditeUnite = d.ValiditeUnite,
        FinDeMois = d.FinDeMois,
        FinDAnnee = d.FinDAnnee,
        RenouvellementNombre = d.RenouvellementNombre,
        RenouvellementUnite = d.RenouvellementUnite,
        AvertissementNombre = d.AvertissementNombre,
        AvertissementUnite = d.AvertissementUnite,
        NbMembresTotal = membres.Count(m =>
            m.Qualifications.Any(q => q.CodeCheck.Equals(d.Code, StringComparison.OrdinalIgnoreCase))),
        NbMembresValides = membres.Count(m =>
            m.Qualifications.Any(q => q.CodeCheck.Equals(d.Code, StringComparison.OrdinalIgnoreCase)
                                      && q.Statut == StatutCheck.Valide)),
        NbMembresExpires = membres.Count(m =>
            m.Qualifications.Any(q => q.CodeCheck.Equals(d.Code, StringComparison.OrdinalIgnoreCase)
                                      && q.Statut == StatutCheck.Expire)),
        NbMembresAvertissement = membres.Count(m =>
            m.Qualifications.Any(q => q.CodeCheck.Equals(d.Code, StringComparison.OrdinalIgnoreCase)
                                      && q.Statut is StatutCheck.Avertissement or StatutCheck.ExpirationProche))
    };
}
