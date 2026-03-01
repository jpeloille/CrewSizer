using CrewSizer.Application.Common.Dtos;
using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Import;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Equipage.Commands;

public record ImportEquipageCommand(
    Stream? StreamPnt,
    Stream? StreamPnc,
    Stream? StreamCheckStatus,
    Stream? StreamCheckDesc
) : IRequest<ImportEquipageResultDto>;

public class ImportEquipageHandler(IDbContextFactory<CrewSizerDbContext> dbFactory) : IRequestHandler<ImportEquipageCommand, ImportEquipageResultDto>
{
    public async Task<ImportEquipageResultDto> Handle(ImportEquipageCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var avertissements = new List<string>();

        // 1. Charger depuis les streams
        var equipage = EquipageLoader.ChargerDepuisStreams(
            request.StreamPnt,
            request.StreamPnc,
            request.StreamCheckStatus,
            request.StreamCheckDesc);

        // 2. Stratégie delete-all + insert (APM = snapshot complet)
        var existingMembres = await db.MembresEquipage.ToListAsync(cancellationToken);
        var existingChecks = await db.DefinitionsCheck.ToListAsync(cancellationToken);

        if (existingMembres.Count > 0)
        {
            db.MembresEquipage.RemoveRange(existingMembres);
            avertissements.Add($"{existingMembres.Count} membres existants remplacés.");
        }
        if (existingChecks.Count > 0)
        {
            db.DefinitionsCheck.RemoveRange(existingChecks);
        }

        // 3. Insérer les nouvelles données
        if (equipage.Membres.Count > 0)
            db.MembresEquipage.AddRange(equipage.Membres);

        if (equipage.Checks.Count > 0)
            db.DefinitionsCheck.AddRange(equipage.Checks);

        await db.SaveChangesAsync(cancellationToken);

        return new ImportEquipageResultDto
        {
            NbMembresImportes = equipage.Membres.Count,
            NbChecksImportes = equipage.Checks.Count,
            DateExtraction = equipage.DateExtraction,
            Avertissements = avertissements
        };
    }
}
