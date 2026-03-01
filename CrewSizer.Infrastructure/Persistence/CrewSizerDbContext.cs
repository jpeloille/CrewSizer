using CrewSizer.Domain.Entities;
using CrewSizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Infrastructure.Persistence;

public class CrewSizerDbContext : IdentityDbContext<ApplicationUser>
{
    public CrewSizerDbContext(DbContextOptions<CrewSizerDbContext> options) : base(options) { }

    public DbSet<Vol> Vols => Set<Vol>();
    public DbSet<BlocVol> BlocsVol => Set<BlocVol>();
    public DbSet<BlocType> BlocTypes => Set<BlocType>();
    public DbSet<TypeAvion> TypesAvion => Set<TypeAvion>();
    public DbSet<SemaineType> SemainesTypes => Set<SemaineType>();
    public DbSet<ConfigurationScenario> Scenarios => Set<ConfigurationScenario>();
    public DbSet<CalculSnapshot> Snapshots => Set<CalculSnapshot>();
    public DbSet<MembreEquipage> MembresEquipage => Set<MembreEquipage>();
    public DbSet<DefinitionCheck> DefinitionsCheck => Set<DefinitionCheck>();
    public DbSet<Competence> Competences => Set<Competence>();
    public DbSet<AffectationEquipage> AffectationsEquipage => Set<AffectationEquipage>();
    public DbSet<DisponibiliteMembre> DisponibilitesMembre => Set<DisponibiliteMembre>();
    public DbSet<HistoriqueHDV> HistoriquesHDV => Set<HistoriqueHDV>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Appliquer les configurations d'abord (OwnsMany/ToJson, etc.)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrewSizerDbContext).Assembly);

        // Convention snake_case pour PostgreSQL (skip entités owned)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Les entités owned (JSON ou inline) sont gérées par leur configuration
            if (entity.IsOwned())
                continue;

            var tableName = entity.GetTableName();
            if (tableName != null)
                entity.SetTableName(ToSnakeCase(tableName));

            foreach (var property in entity.GetProperties())
            {
                var colName = property.GetColumnName();
                if (colName != null)
                    property.SetColumnName(ToSnakeCase(colName));
            }
            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName != null)
                    key.SetName(ToSnakeCase(keyName));
            }
            foreach (var fk in entity.GetForeignKeys())
            {
                var fkName = fk.GetConstraintName();
                if (fkName != null)
                    fk.SetConstraintName(ToSnakeCase(fkName));
            }
            foreach (var index in entity.GetIndexes())
            {
                var idxName = index.GetDatabaseName();
                if (idxName != null)
                    index.SetDatabaseName(ToSnakeCase(idxName));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(name[i - 1]))
                    sb.Append('_');
                else if (i > 0 && i + 1 < name.Length && char.IsUpper(name[i - 1]) && !char.IsUpper(name[i + 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
