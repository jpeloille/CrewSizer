using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class AffectationEquipageConfig : IEntityTypeConfiguration<AffectationEquipage>
{
    public void Configure(EntityTypeBuilder<AffectationEquipage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BlocCode).HasMaxLength(50);
        builder.Property(e => e.Commentaire).HasMaxLength(500);

        builder.HasIndex(e => new { e.MembreId, e.Date });

        // Propriétés calculées ignorées
        builder.Ignore(e => e.EstProductif);
        builder.Ignore(e => e.EstIndisponible);
    }
}
