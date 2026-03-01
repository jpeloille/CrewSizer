using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class DisponibiliteMembreConfig : IEntityTypeConfiguration<DisponibiliteMembre>
{
    public void Configure(EntityTypeBuilder<DisponibiliteMembre> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.MembreCode).HasMaxLength(20);
        builder.Property(e => e.Commentaire).HasMaxLength(500);

        builder.HasIndex(e => e.MembreId);

        // Propriétés calculées ignorées
        builder.Ignore(e => e.NbJours);
        builder.Ignore(e => e.EstActive);
        builder.Ignore(e => e.MotifLibelle);
    }
}
