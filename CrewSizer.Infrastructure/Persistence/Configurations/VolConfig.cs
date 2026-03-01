using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class VolConfig : IEntityTypeConfiguration<Vol>
{
    public void Configure(EntityTypeBuilder<Vol> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Numero).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Depart).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Arrivee).HasMaxLength(10).IsRequired();
        builder.Property(e => e.HeureDepart).HasMaxLength(5);
        builder.Property(e => e.HeureArrivee).HasMaxLength(5);

        builder.HasIndex(e => e.Numero);

        // Ignorer la propriété calculée
        builder.Ignore(e => e.HdvVol);
    }
}
