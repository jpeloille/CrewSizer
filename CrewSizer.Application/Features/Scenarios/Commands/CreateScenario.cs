using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Commands;

public record CreateScenarioCommand(
    string Nom,
    string? Description,
    string? CreePar
) : IRequest<ScenarioDto>;

public class CreateScenarioValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom du scénario est obligatoire.");
    }
}

public class CreateScenarioHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<CreateScenarioCommand, ScenarioDto>
{
    public async Task<ScenarioDto> Handle(CreateScenarioCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var scenario = new ConfigurationScenario
        {
            Nom = request.Nom,
            Description = request.Description,
            CreePar = request.CreePar,
            ModifiePar = request.CreePar
        };

        db.Scenarios.Add(scenario);
        await db.SaveChangesAsync(cancellationToken);

        return scenario.ToDto();
    }
}
