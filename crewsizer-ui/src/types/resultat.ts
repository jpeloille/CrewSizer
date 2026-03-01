// Miroir TS de CrewSizer.Domain.Entities.ResultatMarge
// Les tuples C# (double, bool) sont sérialisés en { item1, item2 } par System.Text.Json

export interface Effectif {
  cdb: number;
  opl: number;
  cc: number;
  pnc: number;
}

export interface VerifCumul {
  item1: number; // cumul
  item2: boolean; // ok
}

export interface VerifLimite {
  item1: number; // valeur
  item2: number; // limite
  item3: boolean; // ok
}

export interface ResultatGroupe {
  nom: string;
  effectif: number;
  capaciteBrute: number;
  totalAbattements: number;
  totalJoursSol: number;
  capaciteNette: number;
  alpha: number;
  hMax: number;
  contrainteMordante: string;
  capaciteNetteHDV: number;
  hdvParPersonne: number;
  verif28j: VerifCumul;
  verif90j: VerifCumul;
  verif12m: VerifCumul;
}

export interface ResultatCategorie {
  nom: string;
  effectif: number;
  capacite: number;
  besoin: number;
  marge: number;
  tauxEngagement: number;
  statut: string;
}

export interface VerifTsvMax {
  nom: string;
  jour: number;
  jourNom: string;
  nbEtapes: number;
  tsvDuree: number;
  tsvMaxAutorise: number;
  conforme: boolean;
}

export interface VerifTempsService {
  totalTSHebdo: number;
  totalTSMensuel: number;
  tsParPersonneHebdo: number;
  verif7j: VerifLimite;
  verif14j: VerifLimite;
  verif28j: VerifLimite;
}

export interface ResumeSemaineJour {
  jour: number;
  jourNom: string;
  nbBlocs: number;
  hdv: number;
  ts: number;
}

export interface DetailProgrammeItem {
  item1: string; // nom
  item2: number; // blocs
  item3: number; // hdv
}

export interface ResultatMarge {
  // Periode
  dateDebut: string;
  dateFin: string;
  libellePeriode: string;
  nbJours: number;

  // Effectifs
  effectifUtilise: Effectif;
  effectifTotal: Effectif;

  // Disponibilite commune
  dDisponible: number;
  joursServiceMaxCycle: number;
  cycle: number;

  // Groupes
  pnt: ResultatGroupe;
  pnc: ResultatGroupe;

  // Sous-categories
  cdb: ResultatCategorie;
  opl: ResultatCategorie;
  cc: ResultatCategorie;
  pncDetail: ResultatCategorie;

  // Programme
  totalBlocs: number;
  totalHDV: number;
  rotations: number;
  etapesParRotation: number;

  // Allocation cabine
  rotationsAvecPNC: number;
  rotationsSansPNC: number;

  // Global
  categorieContraignante: string;
  tauxEngagementGlobal: number;
  statutGlobal: string;

  // Analyses
  nMinPNT: number;
  nMinPNCGroupe: number;
  excedentPNT: number;
  excedentPNCGroupe: number;
  blocsAbsorbables: number;

  // Programme detaille
  detailProgramme: DetailProgrammeItem[];

  // Verifications FTL
  verificationsTSV: VerifTsvMax[];
  tousBlocsConformesTSV: boolean;
  verifTempsServicePNT: VerifTempsService;
  verifTempsServicePNC: VerifTempsService;
  resumeSemaine: ResumeSemaineJour[];
  semainesMois: number;
  nbSemainesPeriode: number;

  // Ventilation mensuelle
  resultatsParMois: ResultatMarge[] | null;

  // Alertes
  alertes: string[];
}
