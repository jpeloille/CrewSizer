using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class CalculSnapshotConfig : IEntityTypeConfiguration<CalculSnapshot>
{
    public void Configure(EntityTypeBuilder<CalculSnapshot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StatutGlobal).HasMaxLength(50);
        builder.Property(e => e.CategorieContraignante).HasMaxLength(50);
        builder.Property(e => e.CalculePar).HasMaxLength(100);
        builder.Property(e => e.ResultatJson).HasColumnType("jsonb");

        builder.HasIndex(e => e.ScenarioId);
        builder.HasIndex(e => e.DateCalcul);
    }
}
