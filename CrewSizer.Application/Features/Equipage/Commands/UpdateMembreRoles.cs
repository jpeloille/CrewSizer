using CrewSizer.Application.Common.Dtos;
using CrewSizer.Application.Common.Mappings;
using CrewSizer.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Commands;

public record UpdateMembreRolesCommand(
    Guid MembreId,
    List<string> Roles
) : IRequest<MembreEquipageDto>;

public class UpdateMembreRolesValidator : AbstractValidator<UpdateMembreRolesCommand>
{
    public UpdateMembreRolesValidator()
    {
        RuleFor(x => x.MembreId).NotEmpty();
        RuleFor(x => x.Roles).NotNull().WithMessage("La liste des rôles est requise.");
    }
}

public class UpdateMembreRolesHandler(IDbContextFactory<CrewSizerDbContext> dbFactory)
    : IRequestHandler<UpdateMembreRolesCommand, MembreEquipageDto>
{
    public async Task<MembreEquipageDto> Handle(UpdateMembreRolesCommand request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.MembresEquipage.FindAsync([request.MembreId], ct)
            ?? throw new KeyNotFoundException($"Membre '{request.MembreId}' introuvable.");

        entity.Roles = request.Roles;
        // Forcer la détection du changement sur la colonne JSONB
        db.Entry(entity).Property(e => e.Roles).IsModified = true;
        await db.SaveChangesAsync(ct);
        return entity.ToDto();
    }
}
