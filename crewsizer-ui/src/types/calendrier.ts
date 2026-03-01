import type { AffectationSemaineDto } from './scenario';

export interface CalendrierDto {
  scenarioId: string;
  affectations: AffectationSemaineDto[];
}
