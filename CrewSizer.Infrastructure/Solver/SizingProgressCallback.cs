using System.Diagnostics;
using Google.OrTools.Sat;

namespace CrewSizer.Infrastructure.Solver;

public sealed class SizingProgressCallback : CpSolverSolutionCallback
{
    private readonly SolveProgressTracker _tracker;
    private readonly string _solveId;
    private readonly string? _category;
    private readonly Stopwatch _sw;
    private int _solutionCount;
    private double _lastImprovementTime;
    private long _lastObjective = long.MaxValue;

    public SizingProgressCallback(
        SolveProgressTracker tracker,
        string solveId,
        string? category,
        Stopwatch stopwatch)
    {
        _tracker = tracker;
        _solveId = solveId;
        _category = category;
        _sw = stopwatch;
    }

    public override void OnSolutionCallback()
    {
        _solutionCount++;
        var objective = (int)ObjectiveValue();
        var elapsed = _sw.Elapsed.TotalSeconds;

        if (objective < _lastObjective)
        {
            _lastObjective = objective;
            _lastImprovementTime = elapsed;
        }

        _tracker.Update(_solveId, new SolveProgress
        {
            SolveId = _solveId,
            Status = "running",
            SolutionsFound = _solutionCount,
            BestObjective = objective,
            ElapsedSeconds = elapsed,
            SecondsSinceLastImprovement = elapsed - _lastImprovementTime,
            CurrentCategory = _category,
        });
    }
}
