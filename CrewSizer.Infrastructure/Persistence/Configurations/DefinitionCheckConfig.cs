using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class DefinitionCheckConfig : IEntityTypeConfiguration<DefinitionCheck>
{
    public void Configure(EntityTypeBuilder<DefinitionCheck> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ValiditeUnite).HasMaxLength(20);
        builder.Property(e => e.RenouvellementUnite).HasMaxLength(20);
        builder.Property(e => e.AvertissementUnite).HasMaxLength(20);

        builder.HasIndex(e => e.Code).IsUnique();
    }
}
