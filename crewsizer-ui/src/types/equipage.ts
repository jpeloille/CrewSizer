import type { Grade, TypeContrat, StatutCheck, GroupeCheck } from './enums';

export interface MembreEquipageDto {
  id: string;
  code: string;
  nom: string;
  actif: boolean;
  contrat: TypeContrat;
  grade: Grade;
  matricule: string;
  dateEntree: string | null;
  dateFin: string | null;
  roles: string[];
  categorie: string;
  typeAvion: string;
  bases: string[];
  statutGlobal: StatutCheck;
  qualificationsResume?: StatutCheck[];
}

export interface QualificationMatrixDto {
  totalMembres: number;
  cdb: number;
  opl: number;
  cc: number;
  pnc: number;
  dateExtraction: string | null;
  membres: MembreEquipageDto[];
}

export interface ImportEquipageResultDto {
  nbMembresImportes: number;
  nbChecksImportes: number;
  dateExtraction: string;
  avertissements: string[];
}

export interface EquipageKpiDto {
  totalMembres: number;
  totalActifs: number;
  cdb: number;
  opl: number;
  cc: number;
  pnc: number;
  alertesExpirees: number;
  alertesProches: number;
  alertesAvertissement: number;
}

export interface MembreDetailDto {
  id: string;
  code: string;
  nom: string;
  actif: boolean;
  contrat: TypeContrat;
  grade: Grade;
  matricule: string;
  dateEntree: string | null;
  dateFin: string | null;
  roles: string[];
  categorie: string;
  typeAvion: string;
  bases: string[];
  qualifications: StatutQualificationDto[];
  nbChecksValides: number;
  nbChecksExpires: number;
  nbChecksAvertissement: number;
  statutGlobal: StatutCheck;
}

export interface StatutQualificationDto {
  codeCheck: string;
  dateExpiration: string | null;
  statut: StatutCheck;
}

export interface DefinitionCheckDto {
  id: string;
  code: string;
  description: string;
  primaire: boolean;
  groupe: GroupeCheck;
  validiteNombre: number;
  validiteUnite: string;
  finDeMois: boolean;
  finDAnnee: boolean;
  renouvellementNombre: number;
  renouvellementUnite: string;
  avertissementNombre: number;
  avertissementUnite: string;
  nbMembresValides: number;
  nbMembresExpires: number;
  nbMembresAvertissement: number;
  nbMembresTotal: number;
}

export interface MatriceCellDto {
  codeCheck: string;
  statut: StatutCheck;
  dateExpiration: string | null;
}

export interface MatriceLigneDto {
  membreId: string;
  code: string;
  nom: string;
  grade: Grade;
  contrat: TypeContrat;
  checks: Record<string, MatriceCellDto>;
}

export interface MatriceQualificationsDto {
  codesChecks: string[];
  lignes: MatriceLigneDto[];
  filtreGroupe: GroupeCheck | null;
}

export interface AlerteQualificationDto {
  membreId: string;
  membreCode: string;
  membreNom: string;
  grade: Grade;
  codeCheck: string;
  descriptionCheck: string;
  statut: StatutCheck;
  dateExpiration: string | null;
  joursRestants: number | null;
}
