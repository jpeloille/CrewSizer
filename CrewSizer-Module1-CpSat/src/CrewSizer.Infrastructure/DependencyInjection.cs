// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Application.Sizing;
using CrewSizer.Infrastructure.Solver;
using Microsoft.Extensions.DependencyInjection;

namespace CrewSizer.Infrastructure;

/// <summary>
/// Extension de DI pour enregistrer les services Infrastructure (solver OR-Tools).
/// Appeler dans Program.cs : builder.Services.AddSolverServices();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSolverServices(this IServiceCollection services)
    {
        services.AddScoped<ISizingSolver, OrToolsSizingSolver>();
        services.AddScoped<SizingService>();

        // Modules 2, 3, 4 seront enregistrés ici plus tard :
        // services.AddScoped<IRosterSolver, OrToolsRosterSolver>();
        // services.AddScoped<ITrainingPlanner, OrToolsTrainingPlanner>();
        // services.AddScoped<ILeaveValidator, OrToolsLeaveValidator>();

        return services;
    }
}
