export interface CalculSnapshotDto {
  id: string;
  scenarioId: string;
  dateCalcul: string;
  calculePar: string | null;
  tauxEngagementGlobal: number;
  statutGlobal: string;
  categorieContraignante: string;
  totalBlocs: number;
  totalHDV: number;
  rotations: number;
  resultatJson: string;
}

export interface CalculSnapshotListItemDto {
  id: string;
  scenarioId: string;
  dateCalcul: string;
  calculePar: string | null;
  tauxEngagementGlobal: number;
  statutGlobal: string;
  categorieContraignante: string;
}
