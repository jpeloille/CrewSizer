using CrewSizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewSizer.Infrastructure.Persistence.Configurations;

public class HistoriqueHDVConfig : IEntityTypeConfiguration<HistoriqueHDV>
{
    public void Configure(EntityTypeBuilder<HistoriqueHDV> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.MembreCode).HasMaxLength(20);

        builder.HasIndex(e => new { e.MembreId, e.DateReleve });
    }
}
