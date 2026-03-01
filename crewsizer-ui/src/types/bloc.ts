export interface BlocVolDto {
  id: string;
  code: string;
  sequence: number;
  jour: string;
  periode: string;
  debutDP: string;
  finDP: string;
  debutFDP: string;
  finFDP: string;
  etapes: EtapeVolDto[];
  blocTypeId?: string;
  blocTypeCode?: string;
  blocTypeLibelle?: string;
  typeAvionId: string;
  typeAvionCode?: string;
  typeAvionLibelle?: string;
  nom: string;
  nbEtapes: number;
  hdvBloc: number;
  dureeTSVHeures: number;
}

export interface EtapeVolDto {
  position: number;
  volId: string;
  modificateur?: number | null;
}
