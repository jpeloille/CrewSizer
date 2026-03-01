export interface ScenarioDto {
  id: string;
  nom: string;
  description: string | null;
  dateCreation: string;
  dateModification: string;
  creePar: string | null;
  modifiePar: string | null;
  dateDebut: string;  // ISO 8601 "2026-01-01"
  dateFin: string;    // ISO 8601 "2026-03-31"
  cdb: number;
  opl: number;
  cc: number;
  pnc: number;
  tsvMaxJournalier: number;
  tsvMoyenRetenu: number;
  reposMinimum: number;
  h28Max: number;
  h90Max: number;
  h12Max: number;
  cumulPntCumul28: number;
  cumulPntCumul90: number;
  cumulPntCumul12: number;
  cumulPncCumul28: number;
  cumulPncCumul90: number;
  cumulPncCumul12: number;
  offReglementaire: number;
  offAccordEntreprise: number;
  tsMax7j: number;
  tsMax14j: number;
  tsMax28j: number;
  fonctionsSolPNT: FonctionSolDto[];
  fonctionsSolPNC: FonctionSolDto[];
  abattementsPNT: AbattementDto[];
  abattementsPNC: AbattementDto[];
  tableTsvMax: EntreeTsvMaxDto[];
  calendrier: AffectationSemaineDto[];
  version: number;
}

export interface ScenarioListItemDto {
  id: string;
  nom: string;
  description: string | null;
  dateModification: string;
  modifiePar: string | null;
  dateDebut: string;
  dateFin: string;
}

export interface FonctionSolDto {
  nom: string;
  nbPersonnes: number;
  joursSolMois: number;
}

export interface AbattementDto {
  libelle: string;
  joursPersonnel: number;
}

export interface EntreeTsvMaxDto {
  debutBande: string;
  finBande: string;
  maxParEtapes: Record<number, number>;
}

export interface AffectationSemaineDto {
  semaine: number;
  annee: number;
  semaineTypeId: string;
  semaineTypeRef: string;
}
