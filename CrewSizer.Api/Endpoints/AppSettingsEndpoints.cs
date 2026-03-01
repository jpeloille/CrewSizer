using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Api.Endpoints;

public static class AppSettingsEndpoints
{
    public static void MapAppSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        group.MapGet("/", async (IDbContextFactory<CrewSizerDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var settings = await db.AppSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync();
            return Results.Ok(settings);
        });

        group.MapGet("/{key}", async (string key, IDbContextFactory<CrewSizerDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            return setting is null ? Results.NotFound() : Results.Ok(setting);
        });

        group.MapPut("/{key}", async (string key, UpdateSettingRequest request, IDbContextFactory<CrewSizerDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var setting = await db.AppSettings.FindAsync(key);
            if (setting is null) return Results.NotFound();

            setting.Value = request.Value;
            await db.SaveChangesAsync();
            return Results.Ok(setting);
        });
    }
}

public record UpdateSettingRequest(string Value);
