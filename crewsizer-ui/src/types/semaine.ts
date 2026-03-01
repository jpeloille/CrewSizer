export interface SemaineTypeDto {
  id: string;
  reference: string;
  saison: string;
  placements: BlocPlacementDto[];
}

export interface BlocPlacementDto {
  blocId: string;
  jour: string;
  sequence: number;
}
