using System.Collections.Concurrent;

namespace CrewSizer.Infrastructure.Solver;

public sealed class SolveProgressTracker
{
    private readonly ConcurrentDictionary<string, SolveProgress> _entries = new();
    private readonly ConcurrentDictionary<string, DateTime> _timestamps = new();

    public void Register(string solveId)
    {
        var progress = new SolveProgress
        {
            SolveId = solveId,
            Status = "running",
        };
        _entries[solveId] = progress;
        _timestamps[solveId] = DateTime.UtcNow;
        Cleanup();
    }

    public void Update(string solveId, SolveProgress progress)
    {
        _entries[solveId] = progress;
    }

    public void Complete(string solveId, Domain.Sizing.CombinedSizingResult result)
    {
        if (_entries.TryGetValue(solveId, out var current))
        {
            _entries[solveId] = current with
            {
                Status = "completed",
                Result = result,
            };
        }
    }

    public void Fail(string solveId, string message)
    {
        if (_entries.TryGetValue(solveId, out var current))
        {
            _entries[solveId] = current with
            {
                Status = "error",
                ErrorMessage = message,
            };
        }
    }

    public SolveProgress? Get(string solveId)
    {
        if (!_entries.TryGetValue(solveId, out var progress))
            return null;

        // Toujours injecter le temps écoulé réel tant que le solve tourne
        if (progress.Status == "running" && _timestamps.TryGetValue(solveId, out var startTime))
        {
            return progress with
            {
                ElapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds,
            };
        }

        return progress;
    }

    /// <summary>Supprime les entrées de plus de 10 minutes.</summary>
    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        foreach (var (key, ts) in _timestamps)
        {
            if (ts < cutoff)
            {
                _entries.TryRemove(key, out _);
                _timestamps.TryRemove(key, out _);
            }
        }
    }
}
