using CrewSizer.Application.Common.Dtos;

namespace CrewSizer.Web.Components.Services;

/// <summary>
/// Service Scoped (par circuit Blazor) pour le scénario actif.
/// </summary>
public class ActiveScenarioService
{
    public ScenarioDto? Scenario { get; private set; }
    public Guid? ScenarioId => Scenario?.Id;
    public bool HasScenario => Scenario is not null;

    public event Action? OnChange;

    public void SetScenario(ScenarioDto? scenario)
    {
        Scenario = scenario;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Scenario = null;
        OnChange?.Invoke();
    }
}
