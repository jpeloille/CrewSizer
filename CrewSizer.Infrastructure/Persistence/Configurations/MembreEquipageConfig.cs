using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class MembreEquipageConfig : IEntityTypeConfiguration<MembreEquipage>
{
    public void Configure(EntityTypeBuilder<MembreEquipage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Nom).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Matricule).HasMaxLength(50);
        builder.Property(e => e.Categorie).HasMaxLength(50);
        builder.Property(e => e.TypeAvion).HasMaxLength(50);

        builder.HasIndex(e => e.Code).IsUnique();

        // Listes string → JSONB
        builder.Property(e => e.Roles).HasColumnType("jsonb");
        builder.Property(e => e.ReglesApplicables).HasColumnType("jsonb");
        builder.Property(e => e.Bases).HasColumnType("jsonb");

        // Qualifications : possédées (JSONB — conservé pour compatibilité CalculateurMarge)
        builder.OwnsMany(e => e.Qualifications, b =>
        {
            b.ToJson("qualifications");
        });
    }
}
