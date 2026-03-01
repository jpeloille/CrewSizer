export type SolverStatus = 'Optimal' | 'Feasible' | 'Infeasible' | 'Timeout' | 'Error';
export type CrewCategory = 'PNT' | 'PNC';
export type CrewRank = 'CDB' | 'OPL' | 'CC' | 'PNC' | 'RPN';
export type ConstraintSource = 'Structural' | 'OroFtl' | 'ConventionCompagnie' | 'Deliberation77';

export interface SizingResult {
  status: SolverStatus;
  message: string;
  solveTimeMs: number;
  isFeasible: boolean;
  minimumCrewByRank: Record<string, number>;
  marginByRank: Record<string, number>;
  criticalDays: CriticalDay[];
  assignments: DailyAssignment[];
  bindingConstraint: string | null;
  bindingConstraintCode: string | null;
  bindingConstraintSource: ConstraintSource | null;
}

export interface CriticalDay {
  date: string;
  rank: CrewRank;
  available: number;
  required: number;
  margin: number;
  reason: string | null;
}

export interface DailyAssignment {
  date: string;
  blockAssignments: BlockAssignment[];
  crewOnDayOff: string[];
}

export interface BlockAssignment {
  blockCode: string;
  assignedCrew: string[];
}

export interface CombinedSizingResult {
  pntResult: SizingResult;
  pncResult: SizingResult;
  isBothFeasible: boolean;
  totalSolveTimeMs: number;
}

export interface SolveProgress {
  solveId: string;
  status: 'running' | 'completed' | 'error';
  solutionsFound: number;
  bestObjective: number;
  elapsedSeconds: number;
  secondsSinceLastImprovement: number | null;
  currentCategory: string | null;
  result: CombinedSizingResult | null;
  errorMessage: string | null;
}
