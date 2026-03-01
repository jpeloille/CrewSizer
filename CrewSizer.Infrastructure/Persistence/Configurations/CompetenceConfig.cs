using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class CompetenceConfig : IEntityTypeConfiguration<Competence>
{
    public void Configure(EntityTypeBuilder<Competence> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Libelle).HasMaxLength(200);

        builder.HasIndex(e => e.Code).IsUnique();

        // ChecksRequis → JSONB
        builder.Property(e => e.ChecksRequis).HasColumnType("jsonb");
    }
}
