using System.Text.Json;
using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class ConfigurationScenarioConfig : IEntityTypeConfiguration<ConfigurationScenario>
{
    public void Configure(EntityTypeBuilder<ConfigurationScenario> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nom).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.CreePar).HasMaxLength(100);
        builder.Property(e => e.ModifiePar).HasMaxLength(100);

        // Concurrence optimiste via xmin PostgreSQL
        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Value Objects inline via OwnsOne
        builder.OwnsOne(e => e.Periode, p =>
        {
            p.Property(x => x.DateDebut).HasColumnName("periode_date_debut");
            p.Property(x => x.DateFin).HasColumnName("periode_date_fin");
            p.Ignore(x => x.NbJours);
            p.Ignore(x => x.LibellePeriode);
        });

        builder.OwnsOne(e => e.Effectif, p =>
        {
            p.Property(x => x.Cdb).HasColumnName("effectif_cdb");
            p.Property(x => x.Opl).HasColumnName("effectif_opl");
            p.Property(x => x.Cc).HasColumnName("effectif_cc");
            p.Property(x => x.Pnc).HasColumnName("effectif_pnc");
        });

        builder.OwnsOne(e => e.LimitesFTL, p =>
        {
            p.Property(x => x.TsvMaxJournalier).HasColumnName("ftl_tsv_max_journalier");
            p.Property(x => x.TsvMoyenRetenu).HasColumnName("ftl_tsv_moyen_retenu");
            p.Property(x => x.ReposMinimum).HasColumnName("ftl_repos_minimum");
        });

        builder.OwnsOne(e => e.LimitesCumulatives, p =>
        {
            p.Property(x => x.H28Max).HasColumnName("cumul_h28_max");
            p.Property(x => x.H90Max).HasColumnName("cumul_h90_max");
            p.Property(x => x.H12Max).HasColumnName("cumul_h12_max");
            p.OwnsOne(x => x.CumulPNT, c =>
            {
                c.Property(y => y.Cumul28Entrant).HasColumnName("cumul_pnt_28_entrant");
                c.Property(y => y.Cumul90Entrant).HasColumnName("cumul_pnt_90_entrant");
                c.Property(y => y.Cumul12Entrant).HasColumnName("cumul_pnt_12_entrant");
            });
            p.OwnsOne(x => x.CumulPNC, c =>
            {
                c.Property(y => y.Cumul28Entrant).HasColumnName("cumul_pnc_28_entrant");
                c.Property(y => y.Cumul90Entrant).HasColumnName("cumul_pnc_90_entrant");
                c.Property(y => y.Cumul12Entrant).HasColumnName("cumul_pnc_12_entrant");
            });
        });

        builder.OwnsOne(e => e.JoursOff, p =>
        {
            p.Property(x => x.Reglementaire).HasColumnName("jours_off_reglementaire");
            p.Property(x => x.AccordEntreprise).HasColumnName("jours_off_accord_entreprise");
        });

        builder.OwnsOne(e => e.LimitesTempsService, p =>
        {
            p.Property(x => x.Max7j).HasColumnName("ts_max_7j");
            p.Property(x => x.Max14j).HasColumnName("ts_max_14j");
            p.Property(x => x.Max28j).HasColumnName("ts_max_28j");
        });

        // Collections JSONB
        builder.OwnsMany(e => e.FonctionsSolPNT, b => b.ToJson("fonctions_sol_pnt"));
        builder.OwnsMany(e => e.FonctionsSolPNC, b => b.ToJson("fonctions_sol_pnc"));
        builder.OwnsMany(e => e.AbattementsPNT, b => b.ToJson("abattements_pnt"));
        builder.OwnsMany(e => e.AbattementsPNC, b => b.ToJson("abattements_pnc"));
        builder.OwnsMany(e => e.TableTsvMax, b =>
        {
            b.ToJson("table_tsv_max");
            b.Property(x => x.MaxParEtapes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<int, double>>(v, (JsonSerializerOptions?)null)!);
        });

        // Calendrier : relation possédée
        builder.OwnsMany(e => e.Calendrier, b =>
        {
            b.ToJson("calendrier");
        });

        // Navigation CalculSnapshots
        builder.HasMany<CalculSnapshot>()
            .WithOne(s => s.Scenario)
            .HasForeignKey(s => s.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
