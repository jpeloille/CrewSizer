using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Scenarios.Commands;

public record UpdateScenarioCommand(ScenarioDto Scenario) : IRequest<ScenarioDto>;

public class UpdateScenarioValidator : AbstractValidator<UpdateScenarioCommand>
{
    public UpdateScenarioValidator()
    {
        RuleFor(x => x.Scenario.Id).NotEmpty();
        RuleFor(x => x.Scenario.Nom).NotEmpty().WithMessage("Le nom du scénario est obligatoire.");
        RuleFor(x => x.Scenario.DateDebut).NotEmpty().WithMessage("La date de début est obligatoire.");
        RuleFor(x => x.Scenario.DateFin).NotEmpty().WithMessage("La date de fin est obligatoire.");
        RuleFor(x => x.Scenario)
            .Must(s => s.DateFin >= s.DateDebut)
            .WithMessage("La date de fin doit être postérieure ou égale à la date de début.");
        RuleFor(x => x.Scenario)
            .Must(s => (s.DateFin.DayNumber - s.DateDebut.DayNumber + 1) <= 366)
            .WithMessage("La période ne peut pas dépasser 1 an.");
    }
}

public class UpdateScenarioHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<UpdateScenarioCommand, ScenarioDto>
{
    public async Task<ScenarioDto> Handle(UpdateScenarioCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Scenarios.FindAsync([request.Scenario.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario '{request.Scenario.Id}' introuvable.");

        request.Scenario.ApplyToEntity(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
